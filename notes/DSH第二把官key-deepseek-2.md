# DSH 第二把 DeepSeek 官方 key（接口名 `deepseek-2`）

> 2026-10-17 加。用户原话：「把这个配置一下，但是不要覆盖当前的官key。另造一个新的接口」

## 铁律

- **主官 key（`DEEPSEEK_API_KEY`，路由 `deepseek-official`）一个字没动**，仍是默认模型。
- 新 key 单独挂在另一条路由 `deepseek-2` 上，两把 key 互不相干，谁挂着都能用。

## 改了两个文件

### 1. `D:\software\dsh-home\.credentials.yaml`

在 `refs:` 段末尾新增一行（值不在此文档记录）：

```yaml
  DEEPSEEK_API_KEY_2: <新 key>
```

### 2. `D:\software\dsh-home\settings.yaml`

新增 `llm-pi-ai:` 段（**这是官方推荐的自定义 provider 写法**，只有 plain object 递归合并，
不会盖掉 profile 里已有的 `aiocloud`）：

```yaml
llm-pi-ai:
  providers:
    deepseek-2:
      displayName: DeepSeek 官方（2 号 key）
      apiKeyEnv: DEEPSEEK_API_KEY_2
      api: openai-completions
      baseURL: https://api.deepseek.com/v1
      compat:
        thinkingFormat: deepseek
      defaultContextWindow: 1000000
      defaultMaxTokens: 256000
      models:
        - id: deepseek-flash
        - id: deepseek-v4-pro
        - id: deepseek-v4-flash
```

模型目录照内建 `deepseek-official` 抄的，参数对齐（100 万上下文 / maxTokens 393216）。

## 怎么用

1. 对话框左下角 **模型选择器** → 选「DeepSeek 官方（2 号 key）」。
2. 或 **设置 → 模型** 里能看见它，标着「自定义」，可编辑/删除。
3. 换回去就选原来的「DeepSeek」，两边互不影响。

## 已验证（2026-10-17）

- 新 key 打 `GET https://api.deepseek.com/v1/models` → HTTP 200，返回 `deepseek-flash` / `deepseek-v4-pro`。
- 真实对话请求 → HTTP 200，正常出字。
- 用 DSH 自己的 `Config` schema 校验 + `mergeLayers` 模拟最终路由：
  `aiocloud, deepseek-2` 两条并存，**原有路由一个没丢**。
- GUI「设置 → 模型」实际显示：`DeepSeek`、`三千 ai-ocloud（免费·GLM）`、`DeepSeek 官方（2 号 key）`。

## 注意

- `settings.yaml` 是热加载的，改完不用重启。
- 以后在 GUI 模型页点「编辑」再保存这条自定义 provider，会用 GUI 的写法重写 `settings.yaml` 的
  `llm-pi-ai` 段——字段结构可能变，但 `apiKeyEnv: DEEPSEEK_API_KEY_2` 这个引用要留着，key 才不会丢。
- 配置备份：`D:\work\AI产出\配置备份-20261017-193448\`
