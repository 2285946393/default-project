"""Pixiv 半自动上传器 —— 图形界面。

流程三段，对应你实际的做法：
  1. 选图/选文件夹 → 工具读 NovelAI 参数，自动填标题和标签
  2. 你手动去打码 → 回来点「匹配打码图」，工具把打码版挂上去
  3. 点「开始上传」→ 浏览器打开 Pixiv 上传页，逐张填好

打码前后元数据会丢，所以参数必须在第 1 步就存进清单。
"""

from __future__ import annotations

import sys
import threading
import tkinter as tk
from pathlib import Path
from tkinter import filedialog, messagebox, ttk

import core
from core import Item, Manifest


class App(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title("Pixiv 半自动上传器")
        self.geometry("1080x680")
        self.minsize(880, 560)

        self.manifest: Manifest | None = None
        self._build()

    # ---------------- 布局 ----------------

    def _build(self):
        top = ttk.Frame(self, padding=(10, 8))
        top.pack(fill="x")

        ttk.Button(top, text="选文件夹…", command=self.pick_folder).pack(side="left")
        ttk.Button(top, text="选单张图…", command=self.pick_files).pack(side="left", padx=(6, 0))
        ttk.Separator(top, orient="vertical").pack(side="left", fill="y", padx=10)

        ttk.Button(top, text="重新扫描", command=self.rescan).pack(side="left")
        ttk.Button(top, text="匹配打码图", command=self.relink).pack(side="left", padx=(6, 0))
        ttk.Button(top, text="存清单", command=self.save_manifest).pack(side="left", padx=(6, 0))
        ttk.Button(top, text="测试登录", command=self.test_login).pack(side="left", padx=(6, 0))

        self.folder_var = tk.StringVar(value="还没选目录")
        ttk.Label(top, textvariable=self.folder_var, foreground="#666").pack(side="left", padx=12)

        # 表格
        mid = ttk.Frame(self, padding=(10, 0))
        mid.pack(fill="both", expand=True)

        cols = ("title", "tags", "masked", "status")
        self.tree = ttk.Treeview(mid, columns=cols, show="tree headings", height=16)
        self.tree.heading("#0", text="文件")
        self.tree.heading("title", text="标题")
        self.tree.heading("tags", text="标签")
        self.tree.heading("masked", text="打码图")
        self.tree.heading("status", text="状态")
        self.tree.column("#0", width=230)
        self.tree.column("title", width=230)
        self.tree.column("tags", width=300)
        self.tree.column("masked", width=120)
        self.tree.column("status", width=80)
        self.tree.pack(side="left", fill="both", expand=True)

        sb = ttk.Scrollbar(mid, orient="vertical", command=self.tree.yview)
        sb.pack(side="left", fill="y")
        self.tree.configure(yscrollcommand=sb.set)
        self.tree.bind("<<TreeviewSelect>>", self.on_select)

        # 编辑区
        bottom = ttk.LabelFrame(self, text="编辑选中项", padding=8)
        bottom.pack(fill="x", padx=10, pady=(6, 0))

        r1 = ttk.Frame(bottom); r1.pack(fill="x")
        ttk.Label(r1, text="标题", width=6).pack(side="left")
        self.title_var = tk.StringVar()
        ttk.Entry(r1, textvariable=self.title_var).pack(side="left", fill="x", expand=True)
        self.r18_var = tk.BooleanVar()
        ttk.Checkbutton(r1, text="R-18", variable=self.r18_var).pack(side="left", padx=8)
        self.r18g_var = tk.BooleanVar()
        ttk.Checkbutton(r1, text="R-18G", variable=self.r18g_var).pack(side="left")
        self.sexual_var = tk.BooleanVar()
        ttk.Checkbutton(r1, text="性描写", variable=self.sexual_var).pack(side="left", padx=8)
        self.private_var = tk.BooleanVar()
        ttk.Checkbutton(r1, text="不公开", variable=self.private_var).pack(side="left")

        r2 = ttk.Frame(bottom); r2.pack(fill="x", pady=(4, 0))
        ttk.Label(r2, text="标签", width=6).pack(side="left")
        self.tags_var = tk.StringVar()
        ttk.Entry(r2, textvariable=self.tags_var).pack(side="left", fill="x", expand=True)

        r3 = ttk.Frame(bottom); r3.pack(fill="x", pady=(4, 0))
        ttk.Label(r3, text="简介", width=6).pack(side="left")
        self.caption_var = tk.StringVar()
        ttk.Entry(r3, textvariable=self.caption_var).pack(side="left", fill="x", expand=True)

        r4 = ttk.Frame(bottom); r4.pack(fill="x", pady=(6, 0))
        self.strip_var = tk.BooleanVar(value=True)
        ttk.Checkbutton(
            r4, text="上传前清除生成参数（推荐）", variable=self.strip_var
        ).pack(side="left")
        ttk.Label(
            r4, text="不碰原图，只生成 _clean 副本", foreground="#888"
        ).pack(side="left", padx=6)

        r5 = ttk.Frame(bottom); r5.pack(fill="x", pady=(6, 0))
        ttk.Button(r5, text="保存修改", command=self.save_edit).pack(side="left")
        ttk.Button(r5, text="选打码图…", command=self.pick_masked).pack(side="left", padx=(6, 0))
        ttk.Button(r5, text="看参数", command=self.show_meta).pack(side="left", padx=(6, 0))
        ttk.Button(r5, text="跳过此张", command=self.skip_item).pack(side="left", padx=(6, 0))
        ttk.Button(r5, text="重新打开（清打码）", command=self.clear_masked).pack(side="left", padx=(6, 0))

        # 底部操作
        act = ttk.Frame(self, padding=(10, 8))
        act.pack(fill="x")
        ttk.Button(act, text="开始上传（打开浏览器）", command=self.start_upload).pack(side="left")
        self.progress = ttk.Progressbar(act, length=260, mode="determinate")
        self.progress.pack(side="left", padx=12)
        self.status_var = tk.StringVar(value="就绪")
        ttk.Label(act, textvariable=self.status_var).pack(side="left")

    # ---------------- 数据操作 ----------------

    def _refresh_tree(self):
        self.tree.delete(*self.tree.get_children())
        if not self.manifest:
            return
        for idx, item in enumerate(self.manifest.items):
            masked = "✓ 有" if item.masked else "—"
            tags = ", ".join(item.tags[:6])
            if len(item.tags) > 6:
                tags += f" …+{len(item.tags) - 6}"
            self.tree.insert(
                "", "end", iid=str(idx),
                text=item.name, values=(item.title, tags, masked, item.status),
            )
        pend = len(self.manifest.pending())
        self.status_var.set(f"共 {len(self.manifest.items)} 张，待上传 {pend} 张")

    def pick_folder(self):
        d = filedialog.askdirectory(title="选一个装满图的文件夹")
        if not d:
            return
        self.manifest = Manifest(d).load()
        added, total = self.manifest.scan()
        self.manifest.save()
        self.folder_var.set(str(self.manifest.folder))
        self._refresh_tree()
        messagebox.showinfo("扫描完成", f"新增 {added} 张，清单共 {total} 张。\n参数已从 PNG 里提取并存好。")

    def pick_files(self):
        files = filedialog.askopenfilenames(
            title="选要上传的图（可多选）",
            filetypes=[("图片", "*.png *.jpg *.jpeg *.webp"), ("全部", "*.*")],
        )
        if not files:
            return
        folder = Path(files[0]).parent
        self.manifest = Manifest(folder).load()
        added = self.manifest.add_files(list(files))
        self.manifest.save()
        self.folder_var.set(str(self.manifest.folder))
        self._refresh_tree()
        messagebox.showinfo("已加入", f"新增 {added} 张。")

    def rescan(self):
        if not self.manifest:
            return
        added, total = self.manifest.scan()
        self.manifest.save()
        self._refresh_tree()
        self.status_var.set(f"重新扫描：新增 {added} 张，共 {total} 张")

    def relink(self):
        if not self.manifest:
            return
        n = self.manifest.relink_masked()
        self.manifest.save()
        self._refresh_tree()
        messagebox.showinfo("匹配完成", f"找到 {n} 张打码图并挂上了。")

    def save_manifest(self):
        if not self.manifest:
            return
        self.manifest.save()
        self.status_var.set(f"清单已存到 {core.MANIFEST_NAME}")

    def _selected(self) -> Item | None:
        sel = self.tree.selection()
        if not sel or not self.manifest:
            return None
        return self.manifest.items[int(sel[0])]

    def on_select(self, _evt=None):
        item = self._selected()
        if not item:
            return
        self.title_var.set(item.title)
        self.tags_var.set(", ".join(item.tags))
        self.caption_var.set(item.caption)
        self.r18_var.set(item.r18)
        self.r18g_var.set(item.r18g)
        self.sexual_var.set(item.sexual)
        self.private_var.set(item.private)
        self.strip_var.set(item.strip_meta)

    def save_edit(self):
        item = self._selected()
        if not item:
            return
        item.title = self.title_var.get().strip()
        item.tags = [t.strip() for t in self.tags_var.get().split(",") if t.strip()]
        item.caption = self.caption_var.get()
        item.r18 = self.r18_var.get()
        item.r18g = self.r18g_var.get()
        item.sexual = self.sexual_var.get()
        item.private = self.private_var.get()
        item.strip_meta = self.strip_var.get()
        if self.manifest:
            self.manifest.save()
        self._refresh_tree()
        self.tree.selection_set(self.tree.selection())

    def pick_masked(self):
        item = self._selected()
        if not item:
            return
        f = filedialog.askopenfilename(
            title="选这张的打码版",
            initialdir=str(Path(item.src).parent),
            filetypes=[("图片", "*.png *.jpg *.jpeg *.webp"), ("全部", "*.*")],
        )
        if not f:
            return
        item.masked = str(Path(f).resolve())
        if self.manifest:
            self.manifest.save()
        self._refresh_tree()

    def clear_masked(self):
        item = self._selected()
        if not item:
            return
        item.masked = ""
        if self.manifest:
            self.manifest.save()
        self._refresh_tree()

    def skip_item(self):
        item = self._selected()
        if not item:
            return
        item.status = "skipped" if item.status == "pending" else "pending"
        if self.manifest:
            self.manifest.save()
        self._refresh_tree()

    def show_meta(self):
        item = self._selected()
        if not item:
            return
        m = item.meta
        if not m:
            messagebox.showinfo("参数", "这张没有 NovelAI 参数（可能已经被打码工具洗掉了）。")
            return
        text = (
            f"模型：{m.get('model')}\n"
            f"采样器：{m.get('sampler')}\n"
            f"步数：{m.get('steps')}    CFG：{m.get('scale')}\n"
            f"种子：{m.get('seed')}\n"
            f"尺寸：{m.get('width')} × {m.get('height')}\n"
            f"画师：{', '.join(m.get('artists') or []) or '无'}\n\n"
            f"正向：\n{m.get('prompt')}\n\n"
            f"负面：\n{m.get('uc')}"
        )
        win = tk.Toplevel(self)
        win.title(f"参数 — {item.name}")
        win.geometry("760x560")
        txt = tk.Text(win, wrap="word")
        txt.insert("1.0", text)
        txt.configure(state="disabled")
        txt.pack(fill="both", expand=True, padx=8, pady=8)

    # ---------------- 上传 ----------------

    def test_login(self):
        """开一个浏览器确认登录状态，不传任何东西。"""
        self.status_var.set("正在检查登录状态…")
        threading.Thread(target=self._login_worker, daemon=True).start()

    def _login_worker(self):
        try:
            from playwright.sync_api import sync_playwright
            from uploader import AUTH_FILE, UPLOAD_URL, _is_logged_in
        except Exception as e:
            self.after(0, lambda: messagebox.showerror("缺少依赖", str(e)))
            return

        try:
            with sync_playwright() as p:
                b = p.chromium.launch(headless=False)
                kw = {"locale": "ja-JP"}
                if AUTH_FILE.exists():
                    kw["storage_state"] = str(AUTH_FILE)
                ctx = b.new_context(**kw)
                pg = ctx.new_page()
                pg.goto(UPLOAD_URL, wait_until="domcontentloaded")
                pg.wait_for_timeout(3000)

                if _is_logged_in(pg):
                    try:
                        ctx.storage_state(path=str(AUTH_FILE))
                    except Exception:
                        pass
                    self.after(0, lambda: self.status_var.set("已登录 ✓"))
                    self.after(0, lambda: messagebox.showinfo(
                        "已登录", "登录状态有效，可以直接上传。"))
                    b.close()
                    return

                # 没登录，等手动登
                self.after(0, lambda: self.status_var.set("等待手动登录…"))
                import time as _t
                deadline = _t.time() + 300
                ok = False
                while _t.time() < deadline:
                    pg.wait_for_timeout(2000)
                    if _is_logged_in(pg):
                        ok = True
                        break

                if ok:
                    try:
                        ctx.storage_state(path=str(AUTH_FILE))
                    except Exception:
                        pass
                    self.after(0, lambda: self.status_var.set("登录成功，已保存 ✓"))
                    self.after(0, lambda: messagebox.showinfo(
                        "登录成功", f"登录态已存到 {AUTH_FILE.name}，以后不用再登。"))
                else:
                    self.after(0, lambda: self.status_var.set("登录超时"))
                    self.after(0, lambda: messagebox.showwarning(
                        "超时", "5 分钟内没检测到登录。"))
                b.close()
        except Exception as e:
            msg = str(e)
            self.after(0, lambda: self.status_var.set("检查失败"))
            self.after(0, lambda: messagebox.showerror("登录检查出错", msg))

    def start_upload(self):
        if not self.manifest:
            messagebox.showwarning("还没选目录", "先选一个文件夹或几张图。")
            return
        items = self.manifest.pending()
        if not items:
            messagebox.showinfo("没有待上传", "所有图都已上传或跳过。")
            return
        no_mask = [i for i in items if not i.masked]
        if no_mask:
            ok = messagebox.askyesno(
                "有图还没打码",
                f"{len(no_mask)} 张没有打码图，会用原图上传。继续？",
            )
            if not ok:
                return
        self.manifest.save()
        self.progress["maximum"] = len(items)
        self.progress["value"] = 0
        self.status_var.set("正在启动浏览器…")
        threading.Thread(target=self._upload_worker, args=(items,), daemon=True).start()

    def _upload_worker(self, items: list[Item]):
        try:
            from uploader import run_uploads
        except Exception as e:
            self.after(0, lambda: messagebox.showerror("缺少依赖", f"上传模块加载失败：{e}"))
            return

        def on_progress(done: int, total: int, msg: str):
            def apply():
                self.progress["value"] = done
                self.status_var.set(msg)
            self.after(0, apply)

        try:
            run_uploads(items, on_progress=on_progress, headless=False)
            if self.manifest:
                self.manifest.save()
            self.after(0, lambda: self._refresh_tree())
            self.after(0, lambda: messagebox.showinfo("完成", "上传流程结束。清单已更新。"))
        except Exception as e:
            msg = str(e)
            self.after(0, lambda: messagebox.showerror("上传出错", msg))


def main():
    app = App()
    app.mainloop()


if __name__ == "__main__":
    sys.exit(main())
