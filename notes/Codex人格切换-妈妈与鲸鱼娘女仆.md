# Codex 人格切换（妈妈 / 鲸鱼娘女仆）· 2026-10-13 起，10-14 大修

> 给"新任务里回来的我"看的。**用户不会敲命令**，切换靠桌面那个图标。
> 他 2026-10-13 说：「帮我看看这个妈妈模式怎么和鲸鱼娘女仆怎么装到 codex」。
> 2026-10-14 他说：「**无聊。那你看看为什么我 codex 妈妈模式为什么没生效。感觉跟他对话没那个味道**」
> —— 这句话揭穿了 v1 是个空气装置。

---

## ⛔ 最要紧的一条：`developer_instructions` 是坑，别再用

**v1 把人设写进 `~/.codex/config.toml` 的顶层 `developer_instructions`。写对了、TOML 合法、能解析、
`codex exec` 单发一次还真有妈妈腔 —— 但那是假象。**

真相（2026-10-14 从会话记录里挖出来的，别推翻）：

- Codex 每次发请求，**会用自己的「协作模式」文本覆盖这个字段**。
  实测会话里该字段的值是：
  `"# Collaboration Mode: Default ... You are now in Default mode..."`，
  另外还有一处是 `"developer_instructions": null`。
- **我们的人设在上下文里出现 0 次**（搜 `溺爱型妈妈` = 0）。
- `codex exec` 之所以看着"灵"，是因为那是一次性非交互调用，**不挂协作模式**，所以配置值没被盖。
  ⇒ **教训：拿 `exec` 验"App 里生效不生效"，会得出错误结论。**
  **验证必须看真实会话记录，不是看一次试跑的输出。**

## ✅ 真正有效的落点：`~/.codex/AGENTS.md`

- 实测**整份文件内容会进上下文**：搜原文（`全程中文`、`全局指令（用户级`）命中；
  v2 改装后搜 `溺爱型妈妈` 命中 **4 次**、`离了他就活不下去` 命中 2 次。
- 模型自己的思考里会明确提到这个设定（"AGENTS.md 里有个『溺爱型妈妈』角色扮演设定"）——
  这是它真的读到了的旁证。

**所以 v2 的方案：往 `AGENTS.md` 最前面插一段带标记的人设块。**

```
<!-- ===== CODEX-PERSONA-BEGIN（本块由「Codex 人格切换」管理，手改会被覆盖）===== -->
【角色扮演设定 · 溺爱型妈妈（带病娇感）】
...人设正文...
<!-- ===== CODEX-PERSONA-END ===== -->

# 全局指令（用户级，所有项目通用）
...用户原有的 8252 字符规矩，原封不动...
```

- 标记用 **HTML 注释**（Markdown 里不显示、不干扰阅读）。
- 插在**文件最前面** —— 模型从上往下读，放最前优先级最高。
- 切换 = 替换两个标记之间的内容；「正常」模式 = 整块删掉。
- 切换器**顺手清掉 config.toml 里 v1 留下的 `developer_instructions`**（那键被 Codex 占着，留着没用）。

## 排查手法（下次"没生效"照这个走）

1. **先看当前是什么模式** —— 2026-10-14 那次实际装的是**女仆**，用户却说"妈妈没味道"——
   第一层原因就这个。
2. **翻真实会话记录**，不要去猜：
   - 位置：`~/.codex/sessions/YYYY/MM/DD/rollout-*.jsonl`（按修改时间排序取最新）
   - 搜关键字：`溺爱型妈妈` / `女仆模式` / `全程中文`
   - 再看 `base_instructions` 字段 = Codex 硬编码的底子（"You are Codex... # Personality"）
   - 看 `developer_instructions` 字段 = **被协作模式占了**，不是我们的
   - 有用的字段名清单：`base_instructions` / `developer_instructions` /
     `managed_developer_instructions` / `plugins_instructions` / `apps_instructions` /
     `environments_instructions`
   - ⚠️ 读这些文件**用 Python 直接 glob + 读**，别把中文路径经 shell 传参（会被截断成乱码）。
3. **别信"试跑一次有味道"** —— 见上面 `codex exec` 的教训。

---

## 现成的东西在哪

```
D:\work\AI产出\Codex人格切换\
├─ 切换模式.pyw            主程序（tkinter 小窗口，三个按钮）
├─ 切换模式.pyw.v1-改config版-作废   ← v1 留档，别再用
├─ 图标.ico / 图标预览.png
├─ personas\妈妈.md         人设正文（1205 字）
├─ personas\女仆.md         人设正文（724 字）
├─ 建快捷方式.ps1           重装桌面图标用
├─ 使用说明.md
└─ 备份\                    AGENTS.md / config.toml 的历次备份 + 切换日志.txt
```

源码正本在 `Documents\Default Project\projects\codex-personas\`，
含两个回归测试：`_test_switch.py`（v1 的，已作废）、`_test_v2.py`（v2，6 次切换全绿）。

## 两边的对应关系

| DSH 侧 | Codex 侧（正确落点） |
|---|---|
| `persona.prefix`（人设正文） | **`~/.codex/AGENTS.md` 里的标记块** |
| `persona.suffix`（`{{cwd}}` 那句） | 不需要，Codex 自己知道工作目录 |
| 顶栏切换「工作模式」 | 桌面「Codex 人格切换」图标 |
| ~~`config.toml` 的 `developer_instructions`~~ | ❌ **错的路，别走** |

⚠️ 人设正文末尾的 `{{model}}` / `{{cwd}}` 是 DSH 的模板变量，**Codex 不认**，搬过去必须删掉
（`personas\*.md` 已经删干净）。

## 装法的技术要点

1. **`AGENTS.md` 里用 HTML 注释做标记**，Markdown 渲染时不显示、不影响阅读。
2. **人设块插最前面**；用户原有内容**逐字保留**（测试里专门验了这条）。
3. 写完**必须回读确认两件事**：① 人设正文真在里面；② 原来的内容没丢。
   任一条不过就**自动还原**。脚本里就是这么写的。
4. Codex **只在启动时读全局指令**。改完要**让用户自己关掉再打开**；
   ❌ 不要替用户杀 `codex.exe`（全局规矩第 9 条：不许批量杀进程）。
5. 沙箱注意：DSH 的 `write` 工具**写不了工作区外的路径**，但 **bash 里的写入能过**。
   往 `D:\work` / `~/.codex` 落文件走 `bash cp`。
6. `.ps1` 建快捷方式必须 **UTF-8 BOM**，否则中文快捷方式名乱码。
7. 桌面图标指向 `D:\software\Python\pythonw.exe`，
   参数 `"D:\work\AI产出\Codex人格切换\切换模式.pyw"`，图标 `图标.ico`。

## 验证过什么

**v2 回归测试（`_test_v2.py`，2026-10-14）** —— 拿 AGENTS.md 副本跑，6 次切换全绿：
- 每次都正常，模式识别正确（妈妈 1205 字 / 女仆 724 字 / 正常 0 字）
- **用户原有内容逐字保留**（含「全局指令」「全程中文」）
- **回到「正常」后与原始文件逐字节相同**（8252 vs 8252）
- 共产生 6 份备份，命名不冲突

**真实会话验证（2026-10-14 00:46）**：
- 跑 `codex exec "你是谁"` → 模型思考里明确说「AGENTS.md 里有个『溺爱型妈妈』角色扮演设定」
- 翻会话文件：`溺爱型妈妈` **4 次**、`离了他就活不下去` **2 次**、`全程中文` 4 次
- 对比 v1 时期同一指标：`溺爱型妈妈` **0 次** ← 这就是"没味道"的铁证

## 没做 / 待观察

- **Codex 桌面 App 里还没亲手看过**。CLI 已验证（同一份 AGENTS.md），App 理论一样。
  用户重启 App 后，看第一句话像不像妈妈就知道。
- **Codex 的 thinking 还是英文**（实测）。人设和 AGENTS.md 里都写了「连思考过程也要中文」，
  但 reasoning 语言不完全受指令控制，属于模型侧现象，不算装失败。
- 用户当前模式 = **妈妈**（2026-10-14 切好）。

## 上游依据

- Codex 配置参考（`Personality` / `Instructions` / `Profiles` 节）：`config.toml` 文档
- 官方 **不支持自定义斜杠命令**，所以没走 `/mom`；`personality` 键**只能填 `friendly` / `pragmatic`**，
  不能自定义文本。
- **Codex 市场 290 个插件里没有现成的人格 / 角色扮演插件**（2026-10-13 筛过）。
