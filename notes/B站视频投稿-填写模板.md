# B 站视频投稿 · 填写模板（2026-09-28 定）

> 每次做歌词 PV / 作品演示要投 B 站时照这个填，不用再现场想字段。
> 图片和视频这类"成品投稿"都适用。

---

## 一、固定字段（直接照抄）

| 字段 | 填什么 |
|---|---|
| 标题 | `<歌名> · 歌词动态PV` ／ 演示片用 `<作品名> · 作品演示` |
| 创作声明 | **内容无需标注**（不是 AI 生成、不是转载时选这个） |
| 分区 | **音乐**（歌词 PV / MV 类）；游戏演示选「游戏」，工具类选「科技数码」 |
| 标签 | `歌词PV`、`文字PV`、`<歌名>`、`<原作者/歌手>`、`JIZURA`（后两个按实际替换） |
| 简介 | `用 JIZURA 做的《<歌名>》歌词动态视频。词曲版权归原作者所有。` |
| 封面 | 用系统推荐封面，或从成片里截一帧 |
| 可见范围 | **默认「公开可见」**；不确定版权时改「仅自己可见」 |

**简介那句版权声明一定要留**——歌是别人的，写明归属是最省事的自保方式。

## 二、操作步骤

1. 打开 `https://member.bilibili.com/platform/upload/video/frame`（登录态要在）
2. 点「上传视频」区域 → 选 mp4 → 等「上传完成」（209MB 约 3 分钟）
3. 填上表四个必填项（标题 / 创作声明 / 分区 / 标签）
4. 简介里带版权声明
5. 点「立即投稿」→ 出现「稿件投递成功」
6. 去「内容管理 → 稿件管理」确认：稿件数 +1、状态「转码完成 · 审核中」

## 三、自动化填写要点（踩过的坑）

1. **标题框是 B 站自己的组件接管的**。一次性写入的值会被它覆盖，一刷新就退回旧标题。
   必须**逐字敲键盘**（Playwright 的 `pressSequentially`），不能只 `fill`。
2. **标签区会残留上次的标签**（比如 `hades2`、`游戏视频`）。要先删干净再加新的：
   `#tag-container .label-item-v2-container svg.close` 逐个点掉，再用输入框 + 回车加新的。
3. **「更多设置」（含可见范围）在窄视口里展不开**，需要把浏览器视口调到
   1500×1000 左右才会正常显示；公开投稿用不到它，可以不碰。
4. 分区是自定义级联组件，点 `.video-human-type .select-controller` 展开后选「音乐」。
5. 创作声明点 `input[placeholder="请选择符合您视频内容的创作声明"]` 展开后选「内容无需标注」。
6. **视频文件保存不了**是因为要给系统「另存为」窗口，自动化点不动——这一步必须真人点。

## 四、可复用的填写片段（在浏览器自动化里执行）

```js
// 标题（必须逐字敲）
const t = tab.playwright.locator('input[placeholder="请输入稿件标题"]').first();
await t.click(); await t.press("Control+a");
await t.pressSequentially("滥俗的歌 · 歌词动态PV", { timeoutMs: 20000 });

// 创作声明
await tab.playwright.locator('input[placeholder="请选择符合您视频内容的创作声明"]').first().click();
await tab.playwright.getByText("内容无需标注", { exact: true }).first().click();

// 分区 → 音乐
await tab.playwright.locator(".video-human-type .select-controller").first().click();
await tab.playwright.getByText("音乐", { exact: true }).first().click();

// 标签：先清旧，再加新
for (const c of (await tab.playwright.locator("#tag-container .label-item-v2-container svg.close").all()).reverse())
  await c.click({ timeoutMs: 3000 }).catch(() => {});
const ti = tab.playwright.locator('input[placeholder="按回车键Enter创建标签"]');
for (const g of ["歌词PV","文字PV","滥俗的歌","汉堡黄","JIZURA"]) {
  await ti.fill(g); await ti.press("Enter"); await tab.playwright.waitForTimeout(400);
}

// 简介（Quill 富文本）
const ed = tab.playwright.locator(".ql-editor").first();
await ed.click(); await ed.press("Control+a");
await ed.pressSequentially("用 JIZURA 做的《滥俗的歌》歌词动态视频。词曲版权归原作者所有。", { timeoutMs: 20000 });

// 投稿
await tab.playwright.getByText("立即投稿", { exact: true }).first().click();
```

## 五、投过的记录

| 日期 | 视频 | 状态 |
|---|---|---|
| 2026-09-28 | 一叶知秋 · 歌词动态PV（Y.Z.H于哲浩 / 李晨曦Chrisu） | 已投，公开 |
| 2026-09-28 | 滥俗的歌 · 歌词动态PV（汉堡黄） | 已投，公开 |
