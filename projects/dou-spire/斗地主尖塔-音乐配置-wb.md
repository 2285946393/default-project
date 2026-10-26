# 斗地主尖塔 · 音乐配置回执（wb）

> 2026-10-08 15:37。用户原话：「Downloads 里面有我下载的音频、我们没有音乐方面。你去配置一下」
> **产物 md5 `c1b95a17d0d5131de1a4e39a608fa2bf`，310,784 B，15:37，本地 == 部署，编译 0 警告 0 错误。**
> **需要重启游戏**（音乐文件本身 F9 收开桌就生效，但下面那个代码修复要重启）。

---

## 一、放了什么 → 放到哪

> ⚠️ **2026-10-10 更正**：下面"欢乐斗地主原声"的说法**只对这一天成立**，后来已换掉。
> 现在包里那 4 首是**从免费商用素材站下载的**，可以随包分发 —— 详见第二节。

用户当时下的是**欢乐斗地主**原声，4 首，都是 60~65 秒的循环曲：

| 源文件（Downloads） | 时长 | 放进哪个槽位 | 什么时候响 |
|---|---|---|---|
| P1 - 主界面 | 65.5s | `music/table/01 主界面.ogg` | 平时打牌 |
| P2 - 游戏开始 | 60.8s | `music/table/02 游戏开始.ogg` | 平时打牌（与上首轮播） |
| P3 - 高潮部分（炸弹 王炸 出时） | 62.7s | `music/climax/03 高潮-炸弹王炸.ogg` | 炸弹/王炸/翻倍/抢地主时插播 |
| P4 - 一张牌 两张牌警告音乐 | 64.1s | `music/tense/04 只剩一两张警告.ogg` | 有人只剩 ≤2 张牌时 |

`win\`（赢）和 `lose\`（输）**故意留空** —— 这两个槽位空着 = 静音，让"天降金币"那几声结算音效自己响，
比塞一首歌更干净（代码里就是这么设计的）。

**转换方式**：用现成的 `tools/import-music.py` 那套（`.m4a` → `.ogg` libvorbis q5 + **EBU R128 响度归一化
到 -16 LUFS**），所以四首音量一致，不会一首吵一首轻。Godot **不认 `.m4a`**，必须先转。

两处都落了（`music-switch.py status` 实测）：
- 工程素材 `projects/dou-spire/assets/music/`（编译时会同步进游戏）
- 游戏目录 `…\Slay the Spire 2\mods\DouSpire\assets\music\`（**直接读这里，立即生效**）
- 原始 `.m4a` 留在 `projects/dou-spire/music-private/`（个人歌库），**Downloads 里的原文件没动**

---

## 二、版权：带歌上架没问题（2026-10-10 修正）

> ⚠️ **这一节以前写的是"上架前必须撤走"** —— 因为当时 `music/table/` 里放的是
> **腾讯欢乐斗地主的原声**。**那个说法已经过时了**：
>
> **2026-10-10 用户确认**：现在 mod 里那 4 首是**从免费商用素材站下载的**，
> 可以随 mod 一起分发。而且**线上 0.2.39 本来就是带歌上的**
> （`D:/software/sts2-mod-uploader/DouSpire/content/assets/music/` 里躺着那 4 个 .ogg）。
> ⇒ **带歌上架 = 和线上现状一致，不需要撤。**

`tools/prepare_workshop.py` 会把 `assets/` 整个打进上传包 —— 所以放什么歌由你决定。

```bash
python tools\music-switch.py status    # 看现在带不带歌
python tools\music-switch.py check     # 报个数（**不再拦上架**，2026-10-10 改）
python tools\music-switch.py public    # 想做"不带歌版"才用：把歌挪进 _撤下备份（不是删）
python tools\music-switch.py private   # 把个人歌库的歌导回来
```

⚠️ **唯一的红线**：你要是**自己换歌**（换成别的来源），得自己确认那首的授权。
免费商用素材站（Pixabay / 海绵音乐 AI 生成等）下的就没问题。

---

## 三、顺带修掉一个「一放歌就会现形」的 bug

**现象**：`music/table/` 里放 **≥2 首**，音乐会**一直在抖**（两首来回切）。

**真因**（`DouSpireMusic.Play`）：
- `UpdateMusic()` 挂在**每次界面刷新**上（很频繁），每次都调 `Play(Table)`；
- 而 `Pick()` 是**随机挑**的，且**刻意避开上一首** —— 槽位里两首时，这就成了**必然交替**：
  每次刷新都挑到"另一首" ⇒ `ReferenceEquals(stream, _nowStream)` 不成立 ⇒
  **每次都触发交叉淡入淡出**，两个播放器互相打断。

以前所有槽位都是空的（`Pick` 直接返回 null → 走本体曲，`Vanilla` 会 early-return），
所以这个坑一直没露出来 —— **一放歌就现形**。

**修法**：加一个 `_nowCue` 记"当前正在服务哪个槽位"，**同一槽位重复 `Play` 立刻返回**；
只有槽位真的换了（平时↔紧张、高潮退场、收开桌）才重新挑歌。
`Stop()` 和 `ClearCache()` 里**必须 `_nowCue = null`** —— 否则收桌再开桌时 `Play` 直接返回，
**音乐再也起不来**（这是个很容易漏的连带坑，已一并处理）。

顺带把"换曲"日志从 `换曲 ?` 改成**打出真实文件名** ——
`AudioStreamOggVorbis.LoadFromFile` 读出来的流没有 `ResourcePath`，以前永远是个问号，没法验收。
现在日志里能直接看到 `斗地主：换曲 01 主界面.ogg`。

---

## 四、怎么验证（交给用户）

1. **先完全退出游戏重开**（dll 换了）。
2. 开桌 → 应该听到 P1 或 P2 之一在循环（两首随机挑一首）。
3. 看日志（可选）：
   ```bash
   grep -a "斗地主音乐：\|斗地主：换曲" "$AppData/SlayTheSpire2/logs/godot.log"
   ```
   预期第一条：`斗地主音乐：table=2 首，tense=1 首，climax=1 首，win=0 首，lose=0 首`
4. 打一局，等**有人只剩 1~2 张牌** → 应切到「只剩一两张警告」。
5. 出一次**炸弹/王炸** → 应插播「高潮-炸弹王炸」，**约 11 秒后自动退回**局势曲。

**怎么调**
- 想换槽位：把 `projects/dou-spire/music-private/<槽位>/` 里的文件挪到另一个槽位文件夹，
  再跑一次 `python tools\music-switch.py private` 就行（**不用重编**）。
- `table` 里放了两首 ⇒ 每次从「紧张」切回「平时」会换一首。**只想固定一首就删掉一个再导入。**
- 高潮曲只插播 **11 秒**（代码常量 `DouSpireMusic.ClimaxSeconds`）。想让炸弹音乐放久点，改这个数。

---

## 五、改动清单

| 文件/目录 | 改了什么 |
|---|---|
| `music-private/{table,tense,climax}/` | 新增 4 首源曲（`.m4a`，个人歌库，不上传） |
| `assets/music/{table,tense,climax}/` | 转换产物 4 个 `.ogg`（**可随包上架，不用撤**；见第二节） |
| `mods/DouSpire/assets/music/`（游戏目录） | 同上，立即生效 |
| `DouSpireCode/Table/DouSpireMusic.cs` | 新增 `_nowCue` 防抖；`Stop`/`ClearCache` 清它；换曲日志打真实文件名 |
