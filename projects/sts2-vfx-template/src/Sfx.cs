// ============================================================================
//  Sfx —— 一个薄薄的音效门面
//
//  为什么要包一层：
//    1. 统一处理"玩家在设置里关了 mod 音效"的情况；
//    2. 包成 try/catch，音效挂了不能让特效也挂；
//    3. 万象辉星另外接了自己的 FMOD bank（RegentFx.bank），
//       如果你也要自定义音频，在这里换成 FmodLite.Play(path) 即可。
//
//  本模板走本体的 SfxCmd.Play(string, float)，零依赖。
//  （依据：万象辉星的 PreventRegentSfx 补丁打的正是 SfxCmd.Play(string, float)，
//    说明这个签名在本体里确实存在、且可被 Harmony 拦。）
// ============================================================================

using System;
using MegaCrit.Sts2.Core.Commands;

namespace YourMod.Scripts.Vfx;

public static class Sfx
{
    /// <summary>玩家设置里"关掉 mod 音效"时，什么都不播</summary>
    public static bool Muted { get; set; }

    public static void Play(string eventPath, float volume = 1f)
    {
        if (Muted || string.IsNullOrEmpty(eventPath)) return;
        try
        {
            SfxCmd.Play(eventPath, volume);
        }
        catch (Exception ex)
        {
            ModEntry.Log($"播音效失败 {eventPath}：{ex.Message}");
        }
    }
}
