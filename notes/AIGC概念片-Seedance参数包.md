# 《28 个，还在继续》· Seedance 2.5 参数包

> 用途：在豆包里逐镜生成，再拼接成 30 秒概念片。
> 每镜**单独生成**（Seedance 单次约 5 秒），共 5 镜。生成完把 mp4 交给有花无实做后期。

---

## 全局设置（每镜都一样）

- **比例**：16:9
- **分辨率**：1080p
- **每镜时长**：5 秒（5 镜 × 5s ≈ 25s，后期微调）
- **统一风格词**（拼在每个正向 prompt 后面）：
  `cinematic, dark background, volumetric light, soft glow, film grain, shallow depth of field, moody, high detail, 4k`
- **全局负向提示词**（Negative）：
  `text, watermark, subtitles, logo, signature, extra limbs, deformed hands, blurry, low resolution, oversaturated, cartoon, anime style, distorted faces`

---

## 镜头 1 · 深夜的房间（0–4s）

**画面**：深夜的卧室，只有一盏台灯和一块发着微光的笔记本屏幕，屏幕上是代码。

**正向 Prompt**：
```
A dark bedroom at night, only a warm desk lamp and a glowing laptop screen with faint code visible, cozy but lonely atmosphere, dust particles floating in the light, cinematic, dark background, volumetric light, soft glow, film grain, shallow depth of field, moody, high detail, 4k
```
**镜头运动**：缓慢推近（slow dolly in）
**负向**：`people, human, face, text, watermark, logo, cartoon, anime`

---

## 镜头 2 · 代码化作光（4–9s）

**画面**：屏幕上的代码变成流动的光，从屏幕里飞出来，散成发光的碎片。

**正向 Prompt**：
```
Code on a laptop screen transforms into flowing streams of light, glowing particles flying out of the screen into the dark room, magical transformation, teal and violet glow, cinematic, dark background, volumetric light, soft glow, film grain, high detail, 4k
```
**镜头运动**：轻微环绕 + 上摇（slow orbit, slight tilt up）
**负向**：`text, watermark, logo, cartoon, anime, low quality`

---

## 镜头 3 · 碎片聚成小世界（9–17s）

**画面**：光点在空中聚成一个个微缩的"世界"——流动的极光、一座发光的塔、一头游动的蓝鲸、一片星尘。

**正向 Prompt**：
```
Glowing particles assemble into miniature floating worlds in the air: swirling aurora fluid, a tall glowing tower, a swimming blue whale, and drifting stardust, dreamy, dark background, volumetric light, soft glow, cinematic, high detail, 4k
```
**镜头运动**：缓慢横移（slow tracking shot）
**负向**：`text, watermark, logo, cartoon, anime, blurry`
**备注**：这一镜的"小世界"**建议用你的真实截图**（aurora / abyss / whale / stardust），只把背景与粒子交给 AI 生成——更真实。

---

## 镜头 4 · 穿过小世界（17–25s）

**画面**：镜头在这些微缩世界之间快速穿行巡游。

**正向 Prompt**：
```
Camera flies through a gallery of glowing miniature worlds floating in dark space, fast dolly forward, aurora colors and stardust, dynamic, cinematic, dark background, volumetric light, motion blur, high detail, 4k
```
**镜头运动**：快速前进穿越（fast dolly forward）
**负向**：`text, watermark, logo, cartoon, anime`
**备注**：同样建议把真实截图（mycelia / particle-life / reaction-diffusion / ink）贴到"世界"上。

---

## 镜头 5 · 回到房间（25–30s）

**画面**：镜头拉远，回到那个深夜的房间，屏幕还亮着，安静而充满希望，缓慢淡出。

**正向 Prompt**：
```
Camera pulls back slowly from a glowing laptop in a dark bedroom at night, the screen still softly glowing, quiet and hopeful mood, slow fade to black, cinematic, dark background, volumetric light, soft glow, film grain, high detail, 4k
```
**镜头运动**：缓慢拉远（slow dolly out）
**负向**：`text, watermark, logo, cartoon, anime`

---

## 后期（有花无实来做）

- **字幕**（别让模型生成文字，会糊）：
  - 开头：`一个 HTML 文件，能做什么？`
  - 结尾：`28 个做完了。第 29 个，正在做。`
- **配乐**：建议安静的钢琴 / 环境音（lofi 或 ambient），最后 2 秒留白。
- **真实截图穿插**：镜头 3、4 里嵌入作品实拍图（见 `assets\作品截图\`）。
- **工具**：剪映 / CapCut 即可，或做成网页版演示（可录屏）。

---

## 一句话提醒

**别全用 AI 生成**——真图 + AI 氛围，才可信。面试官一眼能看出"全 AI"的塑料感，而"真作品 + 一点包装"才是内容运营该有的分寸感。
