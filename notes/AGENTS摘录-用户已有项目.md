# 五、用户已有的项目（改之前先看这里）

> **本文件是从全局声明 `D:\software\dsh-home\AGENTS.md` 搬出来的章节**（2026-10-17 瘦身）。
> 内容是原文一字未改，只是挪了地方。

---

## 五、用户已有的项目（改之前先看这里）

### 博客（最重要）

位置：`D:\work\git\XinghuisamaBlogs\XHBlogs`  
技术栈：Next.js + **Cloudflare Workers**（OpenNext），**不是 Vercel**（已迁走）。

- 线上域名：`ephemeralbloom.dpdns.org`
- 站点配置总入口：`siteConfig.ts`
- 文章：`posts/*.md`（frontmatter 需要 `title / date / tags / mood / cover / description`）
- 封面图：`public/images/`，规格 1600×900
- 相册数据：`data/albums.ts`（34 个相册、约 1000 图、200 MB，6 款游戏全量 Steam 截图）
- 项目矩阵：`data/projects.ts`（有 `demoUrl` 站内直链字段）
- 友链：`data/friends.ts`
- 关于页：`app/about/about.md`
- 16 个作品部署在 `public/lab/`（用 slug 命名）
- 音乐：本地曲库在 `siteConfig.localMusic` + `public/music/`
- 写作控制台：`my-blog-manager\Start.bat`

**发布流程（必须按这个来）：**

1. 文章 md 放进 `posts/`，封面放进 `public/images/`
2. `git add` 指定文件（仓库根是 `D:\work\git\XinghuisamaBlogs`，不是 XHBlogs）
3. `git commit` + `git push origin main`
4. **GitHub 会自动触发 Cloudflare 构建**，约 100~150 秒后生效

⚠️ **不要手动跑 `npm run deploy`**——会和自动部署打架，还会让人搞不清线上是哪一版。  
⚠️ `posts/<slug>` 是请求时读 md 文件的，所以**新文章在部署完成前返回 500（不是 404）**，  
封面返回 404 —— 这两个状态码就说明"构建还没跑完"，不是出错。

#### 🩸 2026-10-10 血泪：**Worker 名字差一个字母，白干两小时**

账号里**有两个名字极像的 Worker**（`wrangler whoami` 里 Account ID `d9e4be28924074043f6f46f084807b15`）：

| Worker 名 | 状态 | 说明 |
|---|---|---|
| `xinghuisamablogs`（**多一个 a**） | ✅ **线上真身** | `ephemeralbloom.dpdns.org` 绑在它上面 |
| `xinghuisanblogs` | ❌ 废弃 | 没绑任何域名，`wrangler.jsonc` 里写的却是它 |

**`wrangler.jsonc` 的 `name` 必须 = `xinghuisamablogs`。**  
传错 Worker 时命令照样打印 `✨ Success! Uploaded`，**但线上一个字节都不会变**。

**铁律（部署完必须做）：**

1. 部署后**必须验证"线上内容真的变了"** —— 不能只看命令退出码。
   ```bash
   curl -s -o /dev/null -w "%{http_code} %{size_download}\n" \
     "https://ephemeralbloom.dpdns.org/posts/<新slug>?v=$RANDOM"
   ```
   或直接 `node_modules/.bin/wrangler dev --port 8799 --local` 本地跑一遍，
   比对**字节数是否与线上一致**（本地 200 而线上 500 ⇒ 100% 是"传错了目标"，不是代码问题）。
2. 查真身别猜：
   ```bash
   # 列出账号里所有 Worker 及各自绑的域名
   curl -H "Authorization: Bearer <wrangler oauth_token>" \
     https://api.cloudflare.com/client/v4/accounts/d9e4be28924074043f6f46f084807b15/workers/domains
   ```
3. **`git push` 才是主路径**（GitHub → Cloudflare 自动构建）。  
   2026-10-10 文章 3 天没上线，根因就是**本机没有 GitHub 凭据、`git push` 一直 401**，  
   所以自动构建根本没被触发 —— 而文档写着"推上去就会自动构建"，没人发现推没上去。
   **推完立刻 `git log origin/main -1` 核对远端 SHA。**

#### 🩸 2026-10-10 血泪：本机沙箱会强杀递归 `fs.cpSync`

任何**递归** `fs.cpSync` 会让 node 进程被直接 kill（RC=127，无异常、`exit` 钩子都不触发）。  
`opennextjs-cloudflare build` 内部用了它 ⇒ 构建中断。  
补丁：`D:\work\AI产出\小站部署\cpsync-shim.cjs`，用 `NODE_OPTIONS=--require=<该文件>` 预载，  
把 `fs.cpSync` 换成手写递归（`mkdirSync` + `copyFileSync`）。**实测可行。**

#### 🩸 2026-10-10 血泪：`next build` 会"静默卡死"，别干等

**正常基准：`next build` 只要 12~18 秒。** 超过 2 分钟且 `.next` 零写入 = 卡死，**立刻杀**。

两个已知根因：

1. **`next.config.ts` 末尾的 `initOpenNextCloudflareForDev()` 在构建期也执行** ——
   它本该只在 `next dev` 跑，构建期会起一个 miniflare 代理并永久等待
   （特征：日志停在 `Using secrets defined in .dev.vars`、`netstat` 看到该 PID **LISTENING**、CPU 近 0）。
   **修法**：包一层 `if (process.env.NODE_ENV === "development")`。
2. **被 kill 的构建会污染 `.next/cache/turbopack`（约 128MB）** ——
   下次构建读缓存时卡住（特征：CPU 10~15% 但零文件写入，最易误判成"慢"）。
   **修法**：`rm -rf .next` 重建，冷缓存也只要 12 秒。

⚠️ **别用 `opennextjs-cloudflare build --skipNextBuild`**：`output: 'standalone'` 是 OpenNext
在跑 `next build` 时注入的，自己手跑会缺 `.next/standalone/.next/server/pages-manifest.json` 报 ENOENT。
**直接让 OpenNext 完整跑**（构建只要 12 秒）：

```bash
NODE_OPTIONS="--require=D:/work/AI产出/小站部署/cpsync-shim.cjs" node_modules/.bin/opennextjs-cloudflare build
node_modules/.bin/wrangler deploy
```

完整版见 `Documents\Default Project\notes\部署验证铁律-20261010.md` 第 4.5 节。

改 `albums` / `projects` / 部署之前，先读 `Documents\Default Project\notes\踩坑笔记.md`。

### 其他

- `pages\作品展馆.html`：16 个作品的入口页（玫瑰粉樱花主题，含 WebGL 封面）
- `pages\深渊塔.html`：15 层爬塔游戏（策略战斗 + 随机事件 + 双结局）
- 其余单文件项目在 `pages/`，多文件工程在 `projects/`

---
