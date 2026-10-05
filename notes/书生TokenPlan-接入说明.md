# 书生 TokenPlan 接入说明

日期：2026-10-04

## 这是什么

上海人工智能实验室（书生那条线）放出来的免费模型额度，页面在
`https://discovery.intern-ai.org.cn/token-plan/`。

它家的钱叫「墨点」。登录之后页面会自动发一笔免费墨点，不用自己点领取。
里面有三个模型是**限时免费（不扣墨点）**：`intern-s2`（书生-S2）、`Agents-A1`、`Atria-Dawn-Preview`。

## 接口地址

| 协议 | 地址 |
|---|---|
| OpenAI 兼容 | `https://discovery-api.intern-ai.org.cn/v1` |
| Anthropic 兼容 | `https://discovery-api.intern-ai.org.cn` |

## 可用模型

限时免费：`intern-s2`、`Agents-A1`、`Atria-Dawn-Preview`

要扣墨点：`deepseek-v4-flash-0731`、`deepseek-v4-flash-vision`、`deepseek-v4-pro-0813`、
`glm-5.3`、`kimi-k2.6`、`minimax-m3`、`qwen3.8-27b`

## 已经配在哪几处

**Codex**

- 供应商写在 `C:\Users\有花无实\.codex\config.toml` 的 `[model_providers.tokenplan]`
- 命令行可以直接用：`codex --profile tokenplan`
- 桌面端要整个切过去，双击 `tools\tokenplan-switch\Codex-切到书生TokenPlan.cmd`
  （切完要**完全关掉 Codex 再打开**）
- 想切回 DeepSeek 官方：双击 `Codex-切回DeepSeek.cmd`

**Claude Code**

- 两套配置模板放在 `C:\Users\有花无实\.claude\`：`settings.deepseek.json` / `settings.tokenplan.json`
- 双击 `tools\tokenplan-switch\ClaudeCode-切到书生TokenPlan.cmd` 切换，切完新开一个终端
- 切回来双击 `ClaudeCode-切回DeepSeek.cmd`

**opencode**

- 供应商写在 `C:\Users\有花无实\.config\opencode\opencode.jsonc` 里，名字叫 `tokenplan`
- 不用切换，直接在 opencode 里选模型，长这样：`tokenplan/deepseek-v4-flash-0731`

## 密钥存在哪

`D:\software\dsh-home\.credentials.yaml` 的 `refs:` 段，名字是 `TOKENPLAN_API_KEY`。

## 注意

- 同一个账号的所有 API Key **共用**额度和限速，不是每个 Key 一份。
- 限速看 RPM / TPM，超了返回 429（`rate_limit_exceeded`），退避重试就行；
  如果是 `quota_exceeded` 说明墨点用完了，重试没用。
- 墨点会到期清零，具体周期在页面上看「我的墨点包」。
- 切换脚本每次都会先把当前配置备份成 `*.bak-switch-<时间>`，改坏了可以翻回去。
