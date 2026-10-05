# Codex 安装进度

日期：2026-09-21
状态：**✅ 全部完成，已登录，可正常使用**

---

## 一、最关键的一个发现：Codex 改名了

官网文档里 Codex 桌面版的微软商店产品 ID 是 `9PLM9XGG6VKS`。
这个 ID 现在在商店里显示的名字是 **ChatGPT**，发布者 OpenAI。

商店描述原文：
> For software development, Codex is the coding agent in ChatGPT

装完之后也印证了——**它的主程序就叫 `ChatGPT.exe`**。

所以：**在商店里搜"Codex"永远搜不到，要搜"ChatGPT"。**

---

## 二、装好了（离线法，绕开商店报错）

商店直接装报 `0x8024001e`，winget download 也超时。
最后走**离线法**，成功。

### 安装包

- 文件：`D:\software\codex\OpenAI.Codex_26.915.4065.0_x64.msix`
- 大小：786 MB
- 版本：`26.915.4065.0`
- 来源：微软官方服务器（通过 store.rg-adguard.net 解析）

### 怎么解析出下载地址的

```
POST https://store.rg-adguard.net/api/GetFiles
body: type=ProductId&url=9PLM9XGG6VKS&ring=RP&lang=en-US
```

注意**必须是 POST**，用 GET 会被 Cloudflare 挡（403）。
拿到的是微软 `tlu.dl.delivery.mp.microsoft.com` 的直链（带签名，有时效）。

### 安装命令

```powershell
Add-AppxPackage -Path "D:\software\codex\OpenAI.Codex_26.915.4065.0_x64.msix"
```

---

## 三、怎么打开

**桌面快捷方式：`ChatGPT Codex.lnk`**（已创建，实测能打开，窗口标题 ChatGPT）

底层启动命令（万一快捷方式丢了）：

```powershell
Start-Process "shell:AppsFolder\OpenAI.Codex_2p2nqsd0c76g0!App"
```

包信息：
- PackageFamilyName：`OpenAI.Codex_2p2nqsd0c76g0`
- AppId：`App`
- 主程序：`ChatGPT.exe`

---

## 四、还差最后一步：登录

App 已经能打开，但**没登录**。

证据：`C:\Users\有花无实\.codex\` 这个目录已经被 App 自动创建了
（22:24 创建，里面有 config.toml、数据库、plugins 等），
但**里面没有 `auth.json`**。

`auth.json` 就是登录凭证文件。没有它 = 没登录。

### 解决办法：用 Codex 认证助手（Chrome 扩展）

仓库：<https://github.com/zhishile/codex-auth-helper>
已克隆到：`D:\software\codex\codex-auth-helper\`

它干什么：**读你 Chrome 里已登录的 ChatGPT 会话 → 导出成 `auth.json`**。
不用重新登录，所以绕过了"收不到验证码"的问题。

**权限很干净**（已核对 manifest.json）：
- `downloads`（保存文件）
- `https://chatgpt.com/`（读会话）

### 操作步骤（Chrome 禁止脚本装扩展，必须手动点）

1. Chrome 地址栏输入 `chrome://extensions/`
2. 右上角打开 **开发者模式**
3. 点左上角 **加载已解压的扩展程序**
4. 选这个文件夹：
   `D:\software\codex\codex-auth-helper\extension`
5. 去 `chatgpt.com` 确认是**已登录**状态
6. 点浏览器右上角拼图图标 → **Codex认证助手** → 点 **生成并保存 auth.json**
7. 把下载到的 `auth.json` 放到 `C:\Users\有花无实\.codex\auth.json`

⚠️ **Chrome 下载目录是 `D:\BaiduNetdiskDownload`**（不是默认的"下载"文件夹），
而且导出的文件会被命名成 `下载.json`，不是 `auth.json`。别找错了。

---

## 五、登录成功了（已验证）

`auth.json` 放进去后重启 App，日志里出现决定性证据：

```
endpoint="/models" auth_header_attached=true auth_mode="Chatgpt"
GET https://chatgpt.com/backend-api/codex/models → status=200 OK
rpc.method="model/list"
```

- `auth_mode = Chatgpt` —— 走的是 **ChatGPT 订阅登录**（不是 API Key）
- `/models` 返回 **200** —— 凭证有效
- 已经在拉 **模型列表** —— 界面正常工作了

### 验证方法（以后想复查）

日志存在 `C:\Users\有花无实\.codex\logs_2.sqlite` 的 `logs` 表。
用 Python 的 `sqlite3` 只读打开，搜 `list_models` 或 `status=200`。

### 唯一的警告（无害）

日志里有几条插件清单警告：

```
ignoring interface.defaultPrompt: maximum of 3 prompts is supported
ignoring interface.defaultPrompt: prompt must be at most 128 characters
```

这是官方自带插件的清单写得不合规，**不影响使用**。

---

## 六、日常怎么用

- **打开**：双击桌面的 **`ChatGPT Codex`** 快捷方式
- 或者开始菜单搜 **ChatGPT**

---

## 七、复盘：这次为什么能成

1. **商店装不上**（`0x8024001e`）→ 改走离线法，从微软服务器直接抠安装包
2. **卡在登录**（收不到验证码）→ 用 Chrome 扩展把浏览器里已有的登录态"搬"过来
3. **验证靠日志**，不靠猜——`logs_2.sqlite` 直接看到了 200

关键点：**没动系统代理设置**，全程绕开了可能出问题的地方。
