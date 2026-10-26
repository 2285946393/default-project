"""Pixiv 上传执行层。

Pixiv 官方 API 没有投稿接口，所以这里用 Playwright 驱动真实的上传页面。

关键设计：
- 首次运行会打开浏览器让你手动登录，登录态存进 pixiv_auth.json，之后免登录。
- 每张图上传完就立刻回写清单，中途崩了不会丢进度。
- 每张之间随机间隔，避免触发风控。
- 上传页 DOM 可能会变，所以选择器写成多候选 + 兜底探测。
"""

from __future__ import annotations

import random
import time
from pathlib import Path
from typing import Callable

AUTH_FILE = Path(__file__).with_name("pixiv_auth.json")
# 实测（2026-10，登录态）：
#   /upload/、/illustration/upload 都是 404
#   /illustration/create 才是真正的投稿表单
#   /upload.php 未登录会被弹回首页
UPLOAD_URL = "https://www.pixiv.net/illustration/create"

# 实测确认可用的选择器（浏览器桥登录态下验证过）
SEL_FILE = [
    'input[type="file"]',
]
SEL_TITLE = [
    'input[aria-label="标题"]',
    'input[placeholder*="タイトル"]',
]
SEL_CAPTION = [
    'textarea[aria-label="说明"]',
    'textarea[placeholder*="キャプション"]',
]
# 标签框的 aria-label 是「必填」，placeholder 里带 0/10
SEL_TAG_INPUT = [
    'input[aria-label="必填"]',
    'input[placeholder*="0/10"]',
    'input[placeholder*="タグ"]',
]
SEL_SUBMIT = [
    'button[type="submit"]',
    'button:has-text("投稿")',
    'button:has-text("投稿する")',
]
# ---- 以下 name 值全部在真实登录态下实测确认（2026-10）----
# 年龄限制
SEL_ALL_AGES = ['input[name="x_restrict"][value="general"]']
SEL_R18 = ['input[name="x_restrict"][value="r18"]']
SEL_R18G = ['input[name="x_restrict"][value="r18g"]']
# 性描写（必填，选完年龄限制才出现）
SEL_SEXUAL_NO = ['input[name="sexual"][value="false"]']
SEL_SEXUAL_YES = ['input[name="sexual"][value="true"]']
# AI 生成作品（必填）
SEL_AI_YES = ['input[name="ai_type"][value="aiGenerated"]']
SEL_AI_NO = ['input[name="ai_type"][value="notAiGenerated"]']
# 公开范围
SEL_PRIVATE = ['input[name="restrict"][value="private"]']
SEL_PUBLIC = ['input[name="restrict"][value="public"]']




def _first(page, selectors: list[str], timeout: int = 8000):
    """按顺序试选择器，返回第一个能用的元素。"""
    last_err = None
    for sel in selectors:
        try:
            loc = page.locator(sel).first
            loc.wait_for(state="visible", timeout=timeout)
            return loc
        except Exception as e:
            last_err = e
    raise RuntimeError(f"找不到元素，试过：{selectors}\n最后错误：{last_err}")


def _is_logged_in(page) -> bool:
    """判断是否已登录。

    实测行为：未登录访问 upload.php 会被弹回首页并带上 return_to 参数，
    不会跳到 accounts.pixiv.net。所以不能只看 URL 里有没有 login。
    """
    url = page.url
    if "return_to=" in url:
        return False
    if "accounts.pixiv.net" in url or "/login" in url:
        return False
    # 页面上有登录按钮 = 没登录
    try:
        if page.locator('a[href*="login"], button:has-text("ログイン")').count() > 0:
            # 主页本身可能有登录入口，用上传表单是否出现来判定更准
            pass
    except Exception:
        pass
    # 最可靠的信号：上传表单里的文件输入框存在
    try:
        return page.locator('input[type="file"]').count() > 0
    except Exception:
        return False


def _ensure_login(context, page, log: Callable[[str], None]) -> None:
    """确保已登录。没登录就等用户手动登。"""
    page.goto(UPLOAD_URL, wait_until="domcontentloaded")
    page.wait_for_timeout(3000)

    if not _is_logged_in(page):
        log("需要登录 —— 请在打开的浏览器里手动登录 Pixiv")
        log("登录完成后不要关窗口，程序会自动继续（最多等 5 分钟）")
        deadline = time.time() + 300
        while time.time() < deadline:
            page.wait_for_timeout(2500)
            if _is_logged_in(page):
                break
        else:
            raise RuntimeError("登录超时（5 分钟）")

        page.goto(UPLOAD_URL, wait_until="domcontentloaded")
        page.wait_for_timeout(3000)
        if not _is_logged_in(page):
            raise RuntimeError("登录后仍看不到上传表单，请检查账号状态")

    # 存登录态，下次直接用
    try:
        context.storage_state(path=str(AUTH_FILE))
        log(f"登录态已保存到 {AUTH_FILE.name}")
    except Exception:
        pass


def _fill_tags(page, tags: list[str], log: Callable[[str], None]) -> None:
    """逐个输入标签，每个回车确认。Pixiv 上限 10 个。"""
    if not tags:
        return
    try:
        box = _first(page, SEL_TAG_INPUT, timeout=6000)
    except RuntimeError:
        log("  ! 没找到标签输入框，跳过标签")
        return
    for t in tags[:10]:
        try:
            box.click()
            box.fill(t)
            page.wait_for_timeout(250)
            page.keyboard.press("Enter")
            page.wait_for_timeout(250)
        except Exception:
            log(f"  ! 标签「{t}」填失败")


def _check_radio(page, selectors: list[str], timeout: int = 3000) -> bool:
    """勾选一个 radio。

    Pixiv 的 radio 本体是隐藏的（用 label 代理点击），所以不能用 visible 判断，
    要等 attached 再 force check。
    """
    for sel in selectors:
        try:
            el = page.locator(sel).first
            el.wait_for(state="attached", timeout=timeout)
            el.check(force=True)
            return True
        except Exception:
            continue
    return False


def _set_age_limit(page, item, log: Callable[[str], None]) -> bool:
    """设置年龄限制。必须最先设，因为「性描写」要等它选完才渲染出来。"""
    if getattr(item, "r18g", False):
        target, label = SEL_R18G, "R-18G"
    elif item.r18:
        target, label = SEL_R18, "R-18"
    else:
        target, label = SEL_ALL_AGES, "全年龄"

    if _check_radio(page, target):
        log(f"  年龄限制：{label}")
        page.wait_for_timeout(1200)     # 等「性描写」行渲染
        return True
    log(f"  ! 年龄限制没设上（要 {label}），请手动确认")
    return False


def _set_sexual(page, item, log: Callable[[str], None]) -> bool:
    """设置「性描写」。这是 Pixiv 的必填项，不选投稿按钮不会亮。"""
    want_yes = bool(getattr(item, "sexual", False))
    target = SEL_SEXUAL_YES if want_yes else SEL_SEXUAL_NO
    label = "有" if want_yes else "无"
    if _check_radio(page, target):
        log(f"  性描写：{label}")
        return True
    log(f"  ! 性描写没设上（要「{label}」），投稿按钮不会亮")
    return False


def _set_ai_flag(page, log: Callable[[str], None]) -> bool:
    """标记为 AI 生成作品。NovelAI 出的图必须声明。"""
    if _check_radio(page, SEL_AI_YES):
        log("  AI 生成作品：是")
        return True
    log("  ! AI 生成标记没点上，请手动勾「AI生成作品 = 是」")
    return False


def _set_privacy(page, item, log: Callable[[str], None]) -> None:
    """公开范围。默认公开，item 里没设就保持默认。"""
    if getattr(item, "private", False):
        if _check_radio(page, SEL_PRIVATE):
            log("  公开范围：不公开")
            return
    log("  公开范围：所有人（默认）")


def _find_submit(page):
    """找真正的投稿按钮。

    页面顶部导航栏也有一个「投稿」按钮（永远是启用的），
    要点的是表单底部那个。用 y 坐标区分：底部那个在页面下方。
    """
    return page.evaluate("""() => {
      const bs = Array.from(document.querySelectorAll('button'))
        .filter(e => (e.innerText || '').trim() === '投稿');
      if (!bs.length) return null;
      // 取页面位置最靠下的那个
      let best = null, bestY = -1;
      for (const e of bs) {
        const y = e.getBoundingClientRect().y + window.scrollY;
        if (y > bestY) { bestY = y; best = e; }
      }
      return { index: bs.indexOf(best), disabled: best.disabled, y: Math.round(bestY) };
    }""")


def _click_submit(page) -> bool:
    """点底部投稿按钮。返回是否点上。"""
    info = _find_submit(page)
    if not info:
        return False
    if info["disabled"]:
        return False
    page.evaluate("""(idx) => {
      const bs = Array.from(document.querySelectorAll('button'))
        .filter(e => (e.innerText || '').trim() === '投稿');
      let best = null, bestY = -1;
      for (const e of bs) {
        const y = e.getBoundingClientRect().y + window.scrollY;
        if (y > bestY) { bestY = y; best = e; }
      }
      if (best) best.click();
    }""", info["index"])
    return True



def _fill_tags(page, tags: list[str], log: Callable[[str], None]) -> int:
    """逐个输入标签，每个回车确认。Pixiv 上限 10 个，且标签是必填项。

    实测：必须用 type（逐字输入）再回车才算数，fill 不会触发 React 的 onChange。
    返回成功加进去的数量。
    """
    if not tags:
        log("  ! 没有标签 —— Pixiv 标签是必填项，投稿按钮不会亮")
        return 0
    try:
        box = _first(page, SEL_TAG_INPUT, timeout=6000)
    except RuntimeError:
        log("  ! 没找到标签输入框")
        return 0

    added = 0
    for t in tags[:10]:
        try:
            box.click()
            box.type(t, delay=60)
            page.wait_for_timeout(600)
            page.keyboard.press("Enter")
            page.wait_for_timeout(700)
            added += 1
        except Exception:
            log(f"  ! 标签「{t}」填失败")
    log(f"  标签：{added} 个")
    return added


def _upload_one(page, item, log: Callable[[str], None]) -> None:
    """上传单张。成功后写回 illust_id。"""
    # 这一步会按需生成剥掉参数的干净副本，不动原图
    path = Path(item.upload_path)
    if not path.exists():
        raise RuntimeError(f"图片不存在：{path}")
    if getattr(item, "strip_meta", True) and path.stem.endswith("_clean"):
        log(f"  已剥掉生成参数 → {path.name}")

    page.goto(UPLOAD_URL, wait_until="domcontentloaded")
    page.wait_for_timeout(3000)

    # 1. 选文件
    file_input = _first(page, SEL_FILE, timeout=20000)
    file_input.set_input_files(str(path))
    log("  已选文件，等缩略图…")
    page.wait_for_timeout(12000)

    # 2. 标题
    try:
        title_box = _first(page, SEL_TITLE, timeout=8000)
        title_box.fill(item.title or path.stem)
        log(f"  标题：{item.title or path.stem}")
    except RuntimeError:
        log("  ! 没找到标题框")

    # 3. 简介
    if item.caption:
        try:
            cap = _first(page, SEL_CAPTION, timeout=4000)
            cap.fill(item.caption)
        except RuntimeError:
            log("  ! 没找到简介框")

    # 4. 标签（必填，先填）
    n_tags = _fill_tags(page, item.tags, log)

    # 5. 年龄限制 → 性描写 → AI 声明（顺序不能乱）
    ok_age = _set_age_limit(page, item, log)
    if ok_age:
        _set_sexual(page, item, log)
    _set_ai_flag(page, log)
    _set_privacy(page, item, log)
    page.wait_for_timeout(1500)

    # 6. 提交前自检
    info = _find_submit(page)
    if not info:
        log("  ! 找不到投稿按钮")
        return
    if info["disabled"]:
        log("  ! 投稿按钮仍是灰的 —— 浏览器停在填好的页面，请手动补全后点投稿")
        return

    if not _click_submit(page):
        log("  ! 点击投稿失败")
        return

    log("  已点投稿，等待结果…")
    for _ in range(12):
        page.wait_for_timeout(2000)
        cur = page.url
        if "/artworks/" in cur or "illust_id" in cur:
            break


    # 7. 尝试抓 illust_id
    try:
        cur = page.url
        if "/artworks/" in cur:
            item.illust_id = cur.rstrip("/").split("/")[-1]
            log(f"  作品 ID：{item.illust_id}")
    except Exception:
        pass

    item.status = "uploaded"


def run_uploads(
    items: list,
    on_progress: Callable[[int, int, str], None] | None = None,
    headless: bool = False,
) -> None:
    """主流程：开浏览器，逐张上传。"""
    from playwright.sync_api import sync_playwright

    def log(msg: str):
        print(msg)
        if on_progress:
            on_progress(current[0], total, msg)

    current = [0]
    total = len(items)

    with sync_playwright() as p:
        browser = p.chromium.launch(headless=headless, args=["--disable-blink-features=AutomationControlled"])
        ctx_kwargs = {"viewport": {"width": 1400, "height": 900}, "locale": "ja-JP"}
        if AUTH_FILE.exists():
            ctx_kwargs["storage_state"] = str(AUTH_FILE)
        context = browser.new_context(**ctx_kwargs)
        page = context.new_page()

        try:
            _ensure_login(context, page, log)
            log(f"开始上传，共 {total} 张")

            for idx, item in enumerate(items, 1):
                current[0] = idx - 1
                log(f"[{idx}/{total}] {item.name}")
                try:
                    _upload_one(page, item, log)
                except Exception as e:
                    log(f"  上传失败：{e}")
                    item.status = "pending"

                current[0] = idx
                if on_progress:
                    on_progress(idx, total, f"完成 {idx}/{total}")

                if idx < total:
                    wait = random.randint(35, 75)
                    log(f"  等 {wait} 秒再传下一张（避免风控）")
                    page.wait_for_timeout(wait * 1000)

            log("全部处理完毕")
        finally:
            try:
                context.storage_state(path=str(AUTH_FILE))
            except Exception:
                pass
            browser.close()
