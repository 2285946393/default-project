# B 站收藏 · 一条命令拉取

> 2026-10-04 打通。以后要翻收藏夹，直接用下面的命令，不用再一步步摸索。

## 怎么用

列收藏夹：

```
node tools/bili-fav.mjs
```

看某个夹里的条目（`--pages 5` = 最近 100 条）：

```
node tools/bili-fav.mjs --items 默认 --pages 5 --json "$env:TEMP\fav.json"
```

登录态过期了（会提示"没找到能用的 B 站登录态"）→ 在浏览器里登录一次 bilibili.com，然后：

```
node tools/bili-fav.mjs --refresh
```

## 它内部干了什么

1. **卷影快照 C 盘**。浏览器把 cookie 库锁死了，直接复制会 Permission denied（连 robocopy /B 都失败）；
   快照拷出来之后立刻删掉快照。
2. 从 `Local State` 用 **DPAPI** 解出 AES 密钥，再解 cookie。Chromium 的格式是
   `v10 | 12 字节 nonce | 密文 | 16 字节 tag`，**而且明文前面还塞了 32 字节的 SHA256(域名)**，得切掉才是真值。
3. 挨个候选登录态调 `x/web-interface/nav` 试，能用才留下；结果缓存进 `%LOCALAPPDATA%\bili-fav\cookie.txt`。
4. 缓存还有效就直接用，**完全不碰浏览器**（实测 0.9 秒出结果）。

## 会扫哪些浏览器

Edge、Chrome 的各个 Profile，另外还有 Playwright 自带的 Chromium
（`%LOCALAPPDATA%\ms-playwright-mcp\*`）和 Codex 内置浏览器。

2026-10-04 实测：**Edge 和 Chrome 里的 SESSDATA 都过期了，能用的是 Playwright 那份**——
所以别看到"浏览器已登录"就直接信，得实际调一次接口确认。

## 坑

- 需要**管理员权限**（卷影快照）。没有的话脚本会退回到直接复制，多半会失败。
- cookie 缓存故意放在仓库外面（`%LOCALAPPDATA%\bili-fav\`）。**别拷进仓库**——
  里面有 SESSDATA，而这个仓库是要 push 到 GitHub 的。
- 收藏夹接口没登录态时返回的是 `{"code":0,"data":null}`，**不报错**，
  很容易误判成"这号没有收藏夹"。先确认 nav 接口 `isLogin: true` 再下结论。

## 相关

- 工具：`tools/bili-fav.mjs`（拉数据）、`tools/bili-fav-extract.ps1`（取登录态）
- 另外两个免费模型入口：`tools/wb.mjs`、`D:\software\dsh-home\lib\free-model.mjs`
