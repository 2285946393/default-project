# chami 站 API 直连 + 参考图限制（完整结论）

> 2026-10-14 深夜实测完成。**这篇是 API 那条线的总账。**
> 配套：`notes/chami站400错误诊断-Vibe编码额度.md`（400 的第一次诊断）

---

## 一、★ 结论先行

| 能力 | 状态 |
|---|---|
| **API 直连出图（不带参考图）** | ✅ **完全可用**，已出图多张 |
| **API 带参考图 / vibe / img2img** | ❌ **一律 400**，站方服务端拦 |
| **带参考图** | 只能走**网页**，且受 **Vibe 编码次数（3/天）** 限制 |

---

## 二、可用的 API（已验证）

```
POST http://chami.yyqzx.com/image/chami/ai/generate-image
（文档写的原始端点是 http://chami.yyqzx.com/image/chami，两个都能通）

Headers:
  Authorization: Bearer {16位用户Key}{内部NAI Key}      ← 拼接，无空格
  Content-Type: application/json
```

### 请求体（NAI 官方结构，已跑通）

```json
{
  "input": "正向提示词",
  "model": "nai-diffusion-4-5-full",
  "action": "generate",
  "parameters": {
    "params_version": 3,
    "width": 832, "height": 1216,
    "scale": 5.0,
    "sampler": "k_euler_ancestral",
    "steps": 28,
    "n_samples": 1,
    "ucPreset": 0,
    "qualityToggle": false,
    "dynamic_thresholding": false,
    "cfg_rescale": 0.2,
    "noise_schedule": "karras",
    "negative_prompt": "负面提示词",
    "v4_prompt": {"caption": {"base_caption": "正向", "char_captions": []},
                  "use_coords": false, "use_order": true},
    "v4_negative_prompt": {"caption": {"base_caption": "负面", "char_captions": []}}
  }
}
```

**响应**：`application/zip` 或 `binary/octet-stream`，ZIP 头 `50 4B 03 04`，解压得 PNG。

### 工具脚本

**`_nai5work/chami_gen.py`** —— 可直接命令行调用：

```bash
python _nai5work/chami_gen.py "正向词" "负面词" 输出名
```

---

## 三、★ 官方文档给的额外字段（妈妈之前漏了）

| 字段 | 说明 |
|---|---|
| **`skip_cfg_above_sigma: 58`** | ★ **多样性开关** —— 网页勾「启用多样性」时传这个，妈妈之前没传 |
| `use_coords: true` | 坐标模式 |
| `ucPreset: 0/1/2/3` | 预设负面配置 |
| `qualityToggle: true` | 质量标签增强 |
| `add_original_image: true` | — |
| `controlnet_strength: 1` | — |
| `extra_noise_seed` | 附加噪声种子（增强多样性） |

**文档明确的限制**：

> **图生图 (`reference_image_multiple`)：最多支持 3 张参考图 base64，超出返回 403**
> ⚠️ 请求格式错误会导致 **502**；在群 7 次/分钟，不在群 3 次/分钟

> 💡 **注意错误码的区分**：
> - **502** = chami 自己网关的错（格式错）
> - **400** = 透传进 NAI 后被拒

---

## 四、参考图为什么一律失败（实测矩阵）

| 测试 | 字段 / 内容 | 结果 |
|---|---|---|
| 原图 base64 ×1 | `reference_image_multiple` | ❌ 400 |
| 原图 base64 ×3 | `reference_image_multiple` | ❌ 400 |
| bundle 里的 `encodings` ×1 | `reference_image_multiple` | ❌ 400 |
| bundle 里的 `encodings` ×3 | `reference_image_multiple` | ❌ 400 |
| 换 6 种字段名 | `reference_images` / `vibe` / `vibe_transfer` / `reference_image` / `reference_image_multiple_v4` | ❌ 全 400 |
| `/ai/encode-vibe` 端点 | snake + camel 两种参数 | ❌ 400 |
| **Director 系列**（NAI 4.5） | `director_reference_images` + `information_extracted` + `strength_values` | ❌ 400 |
| **img2img** | `action: "img2img"` + `image` + `strength` | ❌ 400 |
| `generate` + `image` | — | ❌ 400 |
| **★ 对照组：完全不带图** | — | ✅ **HTTP 200，出图** |

### 判定

**只要 payload 里出现任何图片数据（任何字段名、任何 action），服务端直接拒绝并断连。**
**⇒ 站方对「带图请求」单独校验参考图额度，额度 0 → 一律拒。与字段名、格式、体积无关。**

---

## 五、正确用法（三条线，各管一段）

| 场景 | 走哪条 |
|---|---|
| **不带参考图出图** | ★ **API 直连**（`chami_gen.py`），走「剩余生图 50」线，**不受 Vibe 额度影响** |
| **要参考图（vibe）** | **只能走网页**：氛围模式挂图 → 生成氛围文件 → 点生成。**每天 3 次编码** |
| **已编码好的 `.naiv4vibe` 文件** | 拖进网页「氛围模式」直接加载，**不重新编码**（待验证是否真不扣次数） |

---

## 六、额度真相（2026-10-14 后台实读）

| 项目 | 数值 | 上限 |
|---|---|---|
| 剩余生图次数 | 50 | — |
| GPT 生图次数 | 3000 | 100/天 |
| NAI V5 生图次数 | 187 | 32/天 |
| **Vibe 编码次数** | **0** | **3/天** |
| 累计生图 | 50 | — |
| 在群状态 | 未检测 | 群 `726836268` |

> **每日签到 +50 生图额度**（另一条额度，和 Vibe 编码分开）。

---

## 七、这一轮纠正的误判（留档，别再犯）

| 误判 | 真相 |
|---|---|
| "数字权重 `35::` 超上限导致 400" | ❌ 错。**真因是参考图额度**。权重与本次 400 无关 |
| "chami 站不支持数字权重语法" | ❌ **未证实**，别当事实用 |
| "刷新页面会丢参数" | ⚠️ 半对：**参数和提示词不丢，只有参考图会丢** |
| "绘图区的『重新生成』是危险按钮" | ❌ 错。**危险的是 Dashboard 右上角那个**；绘图区的提交按钮文案会在 `立即生成/再次生成/重新生成` 间变化，是安全的 |

---

## 八、文件清单

| 文件 | 用途 |
|---|---|
| `_nai5work/chami_gen.py` | ★ **可复用出图工具**（API 直连） |
| `_nai5work/_chami_api_test.py` | 鉴权/端点探测 |
| `_nai5work/_chami_probe.py` | 端点存在性探测 |
| `_nai5work/_chami_api_gen.py` | 首次带 vibe 尝试 |
| `_nai5work/_chami_api_step.py` | 分步验证（A 无图 ✅ / B 有图 ❌） |
| `_nai5work/_chami_api_vibe.py` | vibe 体积/格式矩阵 |
| `_nai5work/_chami_api_vibekey.py` | 字段名矩阵 |
| `_nai5work/_chami_vibe_doc.py` | 照官方文档格式重试 |
| `_nai5work/_chami_director.py` | Director 系列字段 |
| `_nai5work/_chami_img2img.py` | ★ img2img + 对照组（决定性证据） |
| `_nai5work/api_out/` | 出图落盘目录 |
