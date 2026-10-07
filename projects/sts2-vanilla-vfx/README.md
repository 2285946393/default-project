# sts2-vanilla-vfx —— 杀戮尖塔 2 本体特效素材库（抽取副本）

> **这是什么**：从本机游戏的 `SlayTheSpire2.pck` 里**抽出来的原始特效文件**（文本格式），
> 用来"读、抄、学"。不是可加载的 mod，不参与构建。
>
> **为什么这么做**：官方教程 3-7 明说了——"如果你完全没有任何素材，也不想使用 Shader/粒子完成特效，
> 就只能最大化利用游戏现有的特效"。而最好的利用方式就是**照着本体抄**。
> 现在 312 个能跑的特效以源码形式躺在本地，不用反编译、不用猜。
>
> **怎么生成的**（可复现）：
> ```bash
> python tools/extract-pck-files.py "<游戏>\SlayTheSpire2.pck" projects/sts2-vanilla-vfx ".gdshader"
> python tools/extract-pck-files.py "<游戏>\SlayTheSpire2.pck" projects/sts2-vanilla-vfx "scenes/vfx"
> python tools/extract-pck-files.py "<游戏>\SlayTheSpire2.pck" projects/sts2-vanilla-vfx "materials"
> python tools/index-vanilla-vfx.py projects/sts2-vanilla-vfx projects/sts2-vanilla-vfx/索引-特效目录.md
> ```
> ⚠️ **游戏更新后重新抽一遍**。这份是本机当前 build 的快照，不是永久事实。

---

## 一、里面有什么

| 目录 | 数量 | 内容 |
|---|---|---|
| `scenes/vfx/` | **312** | 特效场景 `.tscn`，**完整文本节点树**（节点类型、位置、粒子参数、曲线、纹理引用全在） |
| `materials/` | **160** | 材质 `.tres`（`ShaderMaterial`，带着调好的 `shader_parameter/*` 参数值） |
| `shaders/` | **100 + 7** | 着色器 `.gdshader` 源码 + 7 个 `_util/*.gdshaderinc` 可复用函数库 |
| `索引-特效目录.md` | — | 312 个场景的**可检索目录**（构成 + 用途 + shader 链） |

**三层链**：`场景 .tscn` → `材质 .tres` → `着色器 .gdshader`。
看一个效果是"怎么做的"，三层都要翻：场景管结构和位置，材质管参数，shader 管画面算法。

### 纹理不用抽

`images/` 里的 `.png` 在 pck 里已经变成 `.ctex` 二进制，抽出来也没法直接用。
**也不需要** —— 改 mod 时场景引用的是 `res://images/vfx/...`，
pck 里的纹理在运行时照样解析得到。只有你自己要换的纹身才需要新做。

---

## 二、怎么用

### 1. 想找效果 → 搜目录

```bash
grep -i "smoke" "projects/sts2-vanilla-vfx/索引-特效目录.md"
grep -i "distortion" "projects/sts2-vanilla-vfx/索引-特效目录.md"
```

### 2. 想知道怎么做的 → 直接打开场景

```bash
cat "projects/sts2-vanilla-vfx/scenes/vfx/scream/vfx_scream_ring_polar.tscn"
cat "projects/sts2-vanilla-vfx/materials/vfx/scream/vfx_scream_ring_polar.tres"
cat "projects/sts2-vanilla-vfx/shaders/vfx/scream/vfx_scream_ring_polar_shader.gdshader"
```

### 3. 运行时直接用（不用抄任何东西）

```csharp
// 本体特效在 pck 里，路径直接可用
ResourceLoader.Load<PackedScene>("res://scenes/vfx/vfx_attack_slash.tscn");
VfxCmd.PlayVfx(position, "vfx/vfx_attack_slash", container);   // 注意是内部路径
```

### 4. 抄到自己 mod 里改

把 `.tscn` 拷进去 → 改 `res://` 路径指向自己的资源 → 改材质参数 / shader uniform。
**改材质前先 `Duplicate()`**，不然会污染所有共用同一场景缓存的同类特效。

---

## 三、最值得先看的几个（通用件，复用价值最高）

### 粒子通用着色器 `shaders/vfx/common/`

| 文件 | 作用 |
|---|---|
| `vfx_flipbook_shader.gdshader` | 序列帧图集播放（**最常用**） |
| `vfx_row_flipbook_shader.gdshader` | 按行播放的序列帧 |
| `vfx_projectile_flipbook_shader.gdshader` | 投射物专用序列帧 |
| `vfx_common_particle_shader.gdshader` | 粒子通用（渐变/淡出） |
| `vfx_grayscale_particle_shader.gdshader` | 灰度图 + 运行时上色（**一套图出多色**） |
| `vfx_ring_polar_shader.gdshader` | 极坐标环形（冲击波、光环） |
| `vfx_poof_shader.gdshader` | 烟雾消散 |
| `vfx_ray_shader.gdshader` | 光束 |
| `vfx_noise_scale_shader.gdshader` | 噪声扰动 |
| `vfx_panning_shader.gdshader` | UV 平移（流动感） |
| `vfx_screen_chromatic_aberration_shader.gdshader` | 屏幕色差（RGB 分离） |
| `vfx_common_subtractive.gdshader` | 减色混合 |

### 可复用函数库 `shaders/vfx/_util/`（`#include` 就能用）

`flipbook` / `hue_shift` / `polar_coordinates` / `rotate` / `tiling_and_offset` /
`bloat` / `erosion_from_factors`

### 通用工具类着色器 `shaders/`

| 文件 | 作用 |
|---|---|
| `dissolve.gdshader` | 溶解消失（带两张渐变图控制边缘） |
| `hsv.gdshader` | 色相/饱和/明度调整 |
| `radial_blur.gdshader` · `dark_blur.gdshader` · `blur/Blur.gdshader` | 模糊系 |
| `overlay_blend.gdshader` | 叠加混合 |
| `wiggle.gdshader` | 抖动/飘动 |
| `fade_transition.gdshader` · `texture_transition.gdshader` | 转场 |
| `power.gdshader` · `power_flash_vfx.gdshader` | 增益光效 |
| `card_ripple.gdshader` · `relic.gdshader` · `button_pulse.gdshader` | 卡牌/遗物/按钮 |
| `doom_overlay.gdshader` | 末日叠层 |

---

## 四、几个"看源码才知道"的实用发现

1. **翻转书（flipbook）是本体特效的主力技术**。大量攻击/冲击特效就是
   一张网格图 + `vfx_flipbook_shader`，靠 `tiling_and_offset` 逐帧推 UV ——
   比多张 PNG 序列帧省资源，也更好改。
2. **灰度图 + 运行时上色**（`vfx_grayscale_particle_shader`）是本体"一套图出多色"的惯用手法。
   想给同一特效做红/蓝/绿变体，照抄这个，别画三套图。
3. **`materials/*.tres` 里存着调好的参数值**（`shader_parameter/tiling`、`x_offsets` 之类）。
   自己接 shader 时把这些值抄过去，能少调半天。
4. **`scenes/vfx/templates/` 下有三套官方模板**（粒子 / Spine / 序列帧），是新特效的最佳起点。
5. 有些场景本体文件几乎是空的（只有 1 个根节点 + 脚本），**行为写在 C# 里**
   （`MegaCrit.Sts2.Core.Nodes.Vfx.N*.cs`）。这类看 `索引-特效目录.md` 附录 C。

---

## 五、边界（别当成万能的）

- 只抽了**文本类**资源（场景/材质/着色器）。纹理（`.ctex`）、Spine 二进制（`.spskel`）、
  音频都没抽 —— 需要的话运行时引用本体路径即可。
- 版本快照：本机当前 build。游戏更新后路径和节点结构都可能变，**重新抽**。
- 这是**个人本地参考用**的抽取副本，别往外分发（跟反编译源码同一个性质）。

---

相关文档：
- `notes/杀戮尖塔2-Mod特效方法手册.md` —— 做法总表（三条路怎么选、怎么挂、怎么排错）
- `notes/杀戮尖塔2-本体特效资源清单.md` —— 本体资源路径全表
- `projects/sts2-moddev-workspace/` —— 官方/社区教程资料包（含特效教程 7 节 + 反编译源码）
