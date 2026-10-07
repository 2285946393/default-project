// ============================================================================
//  CardFx —— 卡牌特效基类 + 特性注册表
//  来源：反编译自万象辉星(RegentFX) v0.5.1 的
//        Scripts/Vfx/FX.cs + Vfx/Cards/CardFX.cs + CardFxAttribute.cs
//  用法：加一张卡的特效 = 写一个类 + 打一个 [CardFx(typeof(卡))] 标记，别的都不用改
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace YourMod.Scripts.Vfx;

/// <summary>「我要预加载哪些资源」。Entry 会反射收集所有实现者的 AssetPaths。</summary>
public interface IWithFxLoad
{
    List<string> AssetPaths { get; }
}

/// <summary>所有特效的根基类。默认只声明一个场景路径。</summary>
public abstract class Fx : IWithFxLoad
{
    /// <summary>这个特效用哪个场景。null 表示不用场景（纯代码生成）。</summary>
    public virtual string? VfxScenePath => null;

    /// <summary>要预加载的资源。默认就是 VfxScenePath，多场景的覆写它。</summary>
    public virtual List<string> AssetPaths
        => VfxScenePath is { } p ? new List<string> { p } : new List<string>();
}

// ============================================================================

/// <summary>把「游戏里的卡」和「我写的特效类」绑起来。</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CardFxAttribute : Attribute
{
    public Type CardType { get; }
    public CardFxAttribute(Type cardType) => CardType = cardType;
}

/// <summary>
/// 卡牌特效基类。
/// 加一张新卡：
///   [CardFx(typeof(LunarBlast))]
///   public class LunarBlastFx : CardFX
///   {
///       public override string VfxScenePath => "res://你的mod/scenes/laser.tscn";
///       public override Vector2 TargetOffset => new(100f, -450f);
///       public override string? HitSfxPath => "event:/你的mod/sfx/hit";
///       public override bool UseV2Patch => true;     // ★ 想接管攻击指令的话，必须 true
///   }
/// </summary>
public abstract class CardFX : Fx
{
    // ---------------- 注册表 ----------------

    protected static bool _registryInitialized;
    public static readonly Dictionary<Type, Type> Registry = new();   // 卡类型 → 特效类型

    public static IEnumerable<Type> GetTypes =>
        typeof(CardFX).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(CardFX)) && !t.IsAbstract && !t.ContainsGenericParameters);

    public static string GetToggleKey(Type type) => "card_" + type.Name;

    public static void EnsureRegistry()
    {
        if (_registryInitialized) return;
        _registryInitialized = true;
        foreach (var t in GetTypes)
            if (t.GetCustomAttributes(typeof(CardFxAttribute), false).FirstOrDefault() is CardFxAttribute a)
                Registry[a.CardType] = t;
    }

    /// <summary>从一张牌找到它的特效实例；没有 / 被玩家关了 → null</summary>
    public static CardFX? FromCard(CardModel? card)
    {
        if (card == null) return null;
        EnsureRegistry();
        if (!Registry.TryGetValue(card.GetType(), out var fxType)) return null;

        var fx = (CardFX?)Activator.CreateInstance(fxType);
        if (fx == null || !fx.Enabled) return null;
        fx.card = card;
        return fx;
    }

    // ---------------- 实例状态 ----------------

    /// <summary>当前正在处理的这张牌</summary>
    public CardModel? card;

    /// <summary>玩家设置里这个特效开着吗（见 Setting.cs）</summary>
    public bool Enabled => Setting.ToggleEnabled(GetToggleKey(GetType()));

    /// <summary>出牌者是不是朝右（决定特效镜像）</summary>
    public bool OwnerFacingRight
    {
        get
        {
            var creature = card?.Owner?.Creature;
            return creature != null && VfxUtil.IsFacingRight(creature);
        }
    }

    // =================================================================
    //  可覆写参数：只想改哪条就覆写哪条，其余走默认值
    // =================================================================

    /// <summary>拿牌时的表现：借用本体的 / 全部借 / 自己画 / 什么都不做</summary>
    public enum HoldingModes { BorrowDefault, BorrowAll, Custom, None }

    public virtual HoldingModes HoldingMode => HoldingModes.BorrowDefault;

    /// <summary>特效相对出牌者的偏移（例：(-260,-400) = 偏左上）</summary>
    public virtual Vector2 TargetOffset => new(-260f, -400f);

    /// <summary>星星之间的间距（用 StarCount 做多点特效时）</summary>
    public virtual float StarSpacing => 40f;

    public virtual float MoveDuration => 0.3f;

    /// <summary>特效持续时间区间（随机取）</summary>
    public virtual (float min, float max) DurationRange => (0.8f, 1.2f);

    /// <summary>拿在手里时的抖动</summary>
    public virtual float ShakeIntensity => 5f;
    public virtual float ShakeSpeed => 20f;

    // ---- 音效 ----
    public virtual string? HoldSfxPath => null;        // 拿牌音
    public virtual string? HitSfxPath => null;         // 命中音
    public virtual string? SecondarySfxPath => null;   // 第二段音

    // ---- 清理 ----
    /// <summary>播完几秒后销毁。★ 别忘了，不然特效会一直堆在场上</summary>
    public virtual float VfxClearDelay => 2f;

    // ---- 闪屏（全屏曝光）----
    public virtual bool HasExposureEffect => false;
    public virtual float ExposurePeak => 1.2f;
    public virtual float ExposureInDuration => 0.1f;
    public virtual float ExposureOutDuration => 0.5f;

    // ---- 接管本体表现 ----
    /// <summary>是否走 V2 流程接管 AttackCommand.Execute。★ 想用 OnBeforeExecute/OnBeforeDamage 必须 true</summary>
    public virtual bool UseV2Patch => false;
    /// <summary>掐掉本体的武器攻击动画</summary>
    public virtual bool ShouldDisableVanillaAttack => true;
    /// <summary>掐掉本体的攻击音效</summary>
    public virtual bool ShouldDisableVanillaSfx => true;

    public virtual string? ChangeHitFx => null;    // 换成别的本体命中特效
    public virtual bool RemoveHitFx => false;      // 干脆不要本体命中特效

    public virtual bool HasOnBeforeExecute => false;
    public virtual bool HasOnBeforeDamage => false;
    public virtual bool HasAfterPlay => false;

    // =================================================================
    //  生命周期钩子：默认什么都不做
    // =================================================================

    /// <summary>玩家把它拿在手上时（HoldingMode == Custom 时用）</summary>
    public virtual void HoldingCustom() { }

    /// <summary>攻击指令真正执行之前</summary>
    public virtual Task OnBeforeExecute() => Task.CompletedTask;

    /// <summary>造成伤害之前（有具体目标）</summary>
    public virtual Task OnBeforeDamage(AttackCommand command, IReadOnlyList<Creature> targets) => Task.CompletedTask;

    /// <summary>整张牌打完</summary>
    public virtual void AfterPlay() { }

    /// <summary>取消出牌</summary>
    public virtual void OnCancel() { }

    /// <summary>多个目标时，算第 index 个目标的位置</summary>
    public virtual Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount)
    {
        var result = basePosition + TargetOffset;
        if (totalCount > 1)
        {
            float start = -(totalCount - 1) * StarSpacing / 2f;
            result.X += start + index * StarSpacing;
        }
        return result;
    }

    public void TryPlayHoldingSfx()
    {
        if (!string.IsNullOrEmpty(HoldSfxPath)) Sfx.Play(HoldSfxPath!);
    }
}
