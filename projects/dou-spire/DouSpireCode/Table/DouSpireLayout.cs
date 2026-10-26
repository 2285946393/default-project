using Godot;

namespace DouSpire.Table;

/// <summary>
/// 三家坐在哪 —— **一张可以随时改的表**，改完不用重编。
///
/// 为什么要单独一张表：以前立绘和 HUD 是**两套互不相干的坐标系统** ——
/// 立绘按"比例锚点"摆，HUD 按"屏幕四角"摆。结果：
/// <list type="bullet">
///   <item>"我"的立绘在左下，头像框却跑到**左上角**；</item>
///   <item>"上家"的立绘在左边，头像框却跑到**右下角**；</item>
///   <item>立绘 0（0.160,0.745 宽 470）和立绘 2（0.190,0.415 宽 430）**都挤在左半边，横向重叠**。</item>
/// </list>
/// 现在**一个座位一行**，立绘和它的 HUD 一起说清楚，不可能再各跑各的。
///
/// 数字都是**屏幕比例**（0 = 最左 / 最上，1 = 最右 / 最下）；
/// 宽高是 **1920 宽基准的像素**，实际按屏幕宽等比缩放，换分辨率不跑位。
///
/// ── 怎么改 ────────────────────────────────────────────────
/// 编辑 <c>mods\DouSpire\assets\layout.txt</c> → 回游戏 **F9 收桌、再 F9 开桌** 立刻生效。
/// **不用重编译，不用重启游戏。** 文件不存在时会自动生成一份带注释的默认值。
/// </summary>
public static class DouSpireLayout
{
    /// <summary>一个座位：立绘摆哪、多大，HUD 摆哪。</summary>
    public sealed class Seat
    {
        /// <summary>立绘中心（屏幕比例）。</summary>
        public float ArtX = 0.5f, ArtY = 0.5f;

        /// <summary>立绘尺寸（1920 宽基准像素）。三家统一，别再一家大一家小。</summary>
        public float ArtW = 240f, ArtH = 300f;

        /// <summary>HUD 中心（屏幕比例）。</summary>
        public float HudX = 0.5f, HudY = 0.5f;
    }

    /// <summary>
    /// 座位 0 = 我，1 = 下家（右），2 = 上家（左）；4 人时 2 = 对家（上）、3 = 上家（左）。
    /// 固定 4 个槽位：3 人模式只用前 3 个，4 人模式全用。
    /// </summary>
    public static Seat[] Seats { get; } = new Seat[4];

    /// <summary>
    /// **德州桌**的座位摆位 —— 仍然是单独一份文件（<c>layout-poker.txt</c>），
    /// 但 **2026-10-10 起默认值和斗地主完全一样**（见 <see cref="PokerDefaults"/>）。
    ///
    /// 保留独立文件的理由：德州中央是主舞台（底池 + 5 张公共牌），
    /// 万一以后又要为它单独挪某两席，改这一个文件就行，不用动斗地主的摆位。
    /// 现在两份内容一致，是刻意的 —— 用户 2026-10-10 拍板：
    /// 「摆位也奇怪。布局其实按斗地主的就行了」。
    /// </summary>
    public static Seat[] PokerSeats { get; } = new Seat[4];

    /// <summary>HUD 固定尺寸（1920 宽基准像素）。三家同一个尺寸 = "每个 UI 元素大小差不多"。</summary>
    public const float HudW = 340f;
    // 2026-10-07：120 装不下"出顺子（5678910JQK）"这种长记录，会从框里溢出来
    //（用户："信息框从上到下"）。加高到 142，最后一行给两行的高度。
    public const float HudH = 142f;

    /// <summary>换算皇冠/勋章偏移时用的基准高度（= 1920 宽的 16:9）。</summary>
    private const float RefH = 1080f;

    /// <summary>摆位文件的绝对路径。</summary>
    public static string FilePath => string.IsNullOrEmpty(DouSpireAssets.RootDir)
        ? ""
        : DouSpireAssets.RootDir + "/layout.txt";

    /// <summary>德州桌摆位文件的绝对路径（独立一份，见 <see cref="PokerSeats"/> 的注释）。</summary>
    public static string PokerFilePath => string.IsNullOrEmpty(DouSpireAssets.RootDir)
        ? ""
        : DouSpireAssets.RootDir + "/layout-poker.txt";

    private static Seat[] Defaults(int playerCount)
    {
        // 摆位规则（2026-10-07 二改，用户："角色头像被角色挡住了。有的角色太高了，
        // 稍微离远一点。三个角色框就放在左下 / 右上 / 左上，然后你再放人物的立绘"）：
        // **HUD 钉在三个角上，立绘整体再往下让一截** —— 谁高谁矮都压不到头像。
        //
        // ⚠️ 这套数字要和 `assets/layout.txt`（2026-10-09 三改后的那份）**逐行对齐** ——
        //    文件在的时候会覆盖这里，文件丢了/读不了就退回这里；两套不一致的话，
        //    "删掉 layout.txt 重生成"之后牌桌会突然变个样。

        // 我这一席：立绘 0.855（= 原 0.950）。0.950 + 300 高 = 下沿 1.089，掉出屏幕 96px ——
        // 用户 2026-10-09："我的立绘在下面太往下了，半截身子看不到了。往上抬"。
        var me = new Seat { ArtX = 0.115f, ArtY = 0.855f, ArtW = 240f, ArtH = 300f, HudX = 0.115f, HudY = 0.645f };
        var right = new Seat { ArtX = 0.885f, ArtY = 0.450f, ArtW = 240f, ArtH = 300f, HudX = 0.885f, HudY = 0.140f };
        var left = new Seat { ArtX = 0.115f, ArtY = 0.430f, ArtW = 240f, ArtH = 300f, HudX = 0.115f, HudY = 0.140f };

        // 4 人：座位序 我（下）→ 下家（右）→ 对家（上）→ 上家（左），绕着桌子转一圈。
        // 对家坐**右下角**（用户 2026-10-09 拍板："第四个信息和人物到右下"）。
        var top = new Seat { ArtX = 0.890f, ArtY = 0.880f, ArtW = 200f, ArtH = 250f, HudX = 0.885f, HudY = 0.660f };

        // 3 人：座位 2 = **左**（跟摆位文件里的行号含义一致：0 下 / 1 右 / 2 左 / 3 上/右下）。
        // ⚠️ 原来这里两种人数都返回 {我, 右, 上, 左} —— 文件在的时候被覆盖所以看不出来，
        //    文件缺失/解析失败时 3 人桌的"上家"会跑到**屏幕顶上**（跟文档写的"2 = 上家（左）"矛盾）。
        return playerCount >= 4
            ? new[] { me, right, top, left }
            : new[] { me, right, left, top };
    }

    /// <summary>
    /// 德州桌的默认摆位。
    ///
    /// 和斗地主那份的唯一区别在**对家（顶上那一席）**：德州中央要放底池和 5 张公共牌
    /// （y 0.352 / y 0.500，横向占 x 0.35~0.65），所以顶上那一席**缩小并抬高**，
    /// 立绘下沿压在 y≈0.33，正好落在底池上面、不碰公共牌。
    /// 左右两席（x 0.118 / 0.882）本来就避开了中间那一竖条，沿用斗地主的数字。
    /// </summary>
    private static Seat[] PokerDefaults(int playerCount)
    {
        // ★ 2026-10-10（用户："摆位也奇怪。布局其实按斗地主的就行了"）：
        //   德州**直接沿用斗地主那套摆位**，不再自己另搞一份。
        //
        //   旧版是「左右两席贴在桌子中腰（y 0.47）+ 对家坐在顶上正中（0.500, 0.225）」——
        //   四人局时顶上那一席正好压在**底池和 5 张公共牌**那条中轴线上，
        //   桌心被挤得没地方摆东西。这就是用户说的"摆位奇怪"。
        //   斗地主那套是**四个角**（我 左下 / 下家 右上 / 上家 左上 / 对家 右下），
        //   整条中轴（x 0.35~0.65）是空的 —— 正好把桌心完整让给底池和公共牌。
        return Defaults(playerCount);
    }

    /// <summary>
    /// 开桌时读一次。<paramref name="playerCount"/> 决定默认摆位（3 人 / 4 人两套）。
    /// **任何一步出错都退回默认值** —— 摆位不对只是难看，不该把牌桌搞崩。
    /// </summary>
    public static void Load(int playerCount = 3)
    {
        ResetToDefaults(playerCount);
        var applied = ApplyFromFile(FilePath, Seats, playerCount, "斗地主");
        _ = applied;
    }

    /// <summary>
    /// 德州桌开桌时读一次（**独立的一份 <c>layout-poker.txt</c>**，见 <see cref="PokerSeats"/>）。
    /// 语义和 <see cref="Load"/> 完全一样，只是读另一个文件、写进 <see cref="PokerSeats"/>。
    /// </summary>
    public static void LoadPoker(int playerCount = 3)
    {
        var d = PokerDefaults(playerCount);
        for (var i = 0; i < PokerSeats.Length; i++) PokerSeats[i] = d[i];
        ApplyFromFile(PokerFilePath, PokerSeats, playerCount, "德州");
    }

    /// <summary>
    /// 读一份摆位文件写进 <paramref name="target"/>。
    /// **任何一步出错都退回默认值** —— 摆位不对只是难看，不该把牌桌搞崩。
    /// </summary>
    private static int ApplyFromFile(string path, Seat[] target, int playerCount, string who)
    {
        if (string.IsNullOrEmpty(path)) return 0;

        try
        {
            if (!File.Exists(path))
            {
                if (target == PokerSeats) WritePokerDefault(path);
                else WriteDefault(path);
                Entry.Logger.Info($"{who}：没有摆位文件，已生成一份默认的 → {path}");
                return 0;
            }

            // 文件行号按"位置"固定：0 = 我（下）、1 = 右、2 = 左、3 = 上。
            // 3 人模式用行 0/1/2；4 人模式座位序 我→右→上→左，所以
            // 座位 2（对家）读行 3、座位 3（上家）读行 2 —— 左边那行的含义不随人数变。
            var applied = 0;
            var byRow = new Dictionary<int, float[]>();
            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw;
                var hash = line.IndexOf('#');
                if (hash >= 0) line = line[..hash];
                line = line.Trim();
                if (line.Length == 0) continue;

                var eq = line.IndexOf('=');
                if (eq <= 0) continue;

                if (!int.TryParse(line[..eq].Trim(), out var row) || row is < 0 or > 3) continue;

                var nums = line[(eq + 1)..]
                    .Replace(',', ' ')
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (nums.Length < 6) continue;
                if (!float.TryParse(nums[0], out var ax) || !float.TryParse(nums[1], out var ay)
                    || !float.TryParse(nums[2], out var aw) || !float.TryParse(nums[3], out var ah)
                    || !float.TryParse(nums[4], out var hx) || !float.TryParse(nums[5], out var hy))
                    continue;

                byRow[row] = new[] { ax, ay, aw, ah, hx, hy };
            }

            var map = playerCount == 4 ? new[] { 0, 1, 3, 2 } : new[] { 0, 1, 2 };
            for (var seat = 0; seat < playerCount && seat < target.Length; seat++)
            {
                if (!byRow.TryGetValue(map[seat], out var v)) continue;
                var s = target[seat];
                s.ArtX = Mathf.Clamp(v[0], -0.5f, 1.5f);
                s.ArtY = Mathf.Clamp(v[1], -0.5f, 1.5f);
                s.ArtW = Mathf.Clamp(v[2], 40f, 2000f);
                s.ArtH = Mathf.Clamp(v[3], 40f, 2000f);
                s.HudX = Mathf.Clamp(v[4], -0.5f, 1.5f);
                s.HudY = Mathf.Clamp(v[5], -0.5f, 1.5f);
                applied++;
            }

            Entry.Logger.Info($"{who}：摆位文件读进来 {applied}/{playerCount} 个座位 → {path}");
            if (applied == 0)
                Entry.Logger.Warn($"{who}：摆位文件一行都没解析出来（格式见文件里的注释），这次用默认摆位。");
            return applied;
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"{who}：摆位文件读不了，这次用默认摆位（不影响玩）: {e.Message}");
            return 0;
        }
    }

    /// <summary>立绘半高换算成 y 方向的比例（皇冠/勋章定位用）。</summary>
    public static float HalfArtY(Seat s) => s.ArtH / RefH * 0.5f;

    private static void ResetToDefaults(int playerCount)
    {
        var d = Defaults(playerCount);
        for (var i = 0; i < Seats.Length; i++) Seats[i] = d[i];
    }

    private static void WriteDefault(string path)
    {
        // ⚠️ 这里的数字要和 `assets/layout.txt` 里那份**逐行一样** ——
        //    不然"删掉 layout.txt 让它自动重生成"之后牌桌会变个样（2026-10-09 对齐过一次）。
        const string text =
            "# 斗地主尖塔 —— 三家坐在哪\n" +
            "#\n" +
            "# 每行一个座位：\n" +
            "#     座位号 = 立绘x  立绘y  立绘宽  立绘高   hudx  hudy\n" +
            "#\n" +
            "#   x / y   屏幕比例：0 = 最左 / 最上，1 = 最右 / 最下\n" +
            "#   宽 / 高 1920 宽基准的像素（会按屏幕宽等比缩放，换分辨率不跑位）\n" +
            "#\n" +
            "# 座位号（按**位置**固定，两种人数通用）：\n" +
            "#     0 = 我（下）    1 = 下家（右）    2 = 上家（左）    3 = 对家（右下，只有四人模式用）\n" +
            "#\n" +
            "# 改完存盘 → 回游戏按 F9 收桌、再按 F9 开桌 → 立刻生效。\n" +
            "# 不用重编译、不用重启游戏。# 后面都是注释，随便删。\n" +
            "#\n" +
            "# 屏幕纵向占位参考（1920x1080）：\n" +
            "#     y ~0.03      标题条\n" +
            "#     y 0.14       左上 / 右上 HUD\n" +
            "#     y 0.27       底牌 3/4 张（桌图自带的空牌位）\n" +
            "#     y 0.46-0.58  场上最后一手\n" +
            "#     y 0.68       出牌说明文字\n" +
            "#     y 0.78       操作按钮（出牌 / 要不起 / 提示）\n" +
            "#     y 0.91       我的手牌\n" +
            "#     左列：上家立绘(0.43) → 我的信息框(0.645) → 我的立绘(0.855)\n" +
            "#     右列：下家信息框(0.14) → 下家立绘(0.45) → 对家信息框(0.66) → 对家立绘(0.88)\n" +
            "\n" +
            "0 = 0.115 0.855 240 300   0.115 0.645\n" +
            "1 = 0.885 0.450 240 300   0.885 0.140\n" +
            "2 = 0.115 0.430 240 300   0.115 0.140\n" +
            "3 = 0.890 0.880 200 250   0.885 0.660\n";

        WriteText(path, text);
    }

    /// <summary>德州桌的摆位文件 —— ★ 2026-10-10 起与斗地主**同一套数字**（见 <see cref="PokerDefaults"/>）。</summary>
    private static void WritePokerDefault(string path)
    {
        const string text =
            "# 德州扑克尖塔 —— 四家坐在哪\n" +
            "#\n" +
            "# ★ 2026-10-10 起**直接沿用斗地主那套摆位**（用户：\"布局其实按斗地主的就行了\"）。\n" +
            "#   为什么：德州中央是主舞台（底池 + 5 张公共牌），座位必须让开中轴那条竖带。\n" +
            "#   斗地主那套是\"四个角\"（我 左下 / 右 右上 / 左 左上 / 对家 右下），中轴整条是空的。\n" +
            "#   （旧版是左右贴中腰 + 对家坐顶上正中，顶上那一席正好压在底池和公共牌上。）\n" +
            "#\n" +
            "# 每行一个座位：\n" +
            "#     座位号 = 立绘x  立绘y  立绘宽  立绘高   hudx  hudy\n" +
            "#\n" +
            "#   x / y   屏幕比例：0 = 最左 / 最上，1 = 最右 / 最下\n" +
            "#   宽 / 高 1920 宽基准的像素（会按屏幕宽等比缩放，换分辨率不跑位）\n" +
            "#\n" +
            "# 座位号（按**位置**固定，两种人数通用）：\n" +
            "#     0 = 我（下）    1 = 右边    2 = 左边    3 = 右下（只有四人模式用）\n" +
            "#\n" +
            "# ⚠️ 德州桌中央是主舞台，别让座位立绘压上去：\n" +
            "#     5 张公共牌 / 底池 / 状态条 都在 x 0.35~0.65 这条中轴上\n" +
            "#     加注提示  y ≈ 0.702     动作按钮 y ≈ 0.802     我的底牌 y ≈ 0.952\n" +
            "#\n" +
            "# 改完存盘 → 回游戏按 F9 收桌、再按 F9 开桌 → 立刻生效。\n" +
            "# 不用重编译、不用重启游戏。# 后面都是注释，随便删。\n" +
            "\n" +
            "0 = 0.115 0.855 240 300   0.115 0.645\n" +
            "1 = 0.885 0.450 240 300   0.885 0.140\n" +
            "2 = 0.115 0.430 240 300   0.115 0.140\n" +
            "3 = 0.890 0.880 200 250   0.885 0.660\n";

        WriteText(path, text);
    }

    private static void WriteText(string path, string text)
    {
        try
        {
            File.WriteAllText(path, text);
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：摆位文件写不出去（不影响玩，下次开桌再试）: {e.Message}");
        }
    }
}
