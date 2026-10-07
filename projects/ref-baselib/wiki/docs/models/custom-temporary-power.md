---
title: CustomTemporaryPowerModel
parent: Custom Models
---

The `CustomTemporaryPowerModel` is designed to allow you to create temporary versions of any power similar to the base game. It also provides some additional features.

The base game only has Temporary Power Models for `Strength`, `Dexterity` and `Focus`. If you intend to create a temporary power based on them, you can inherit either from the respective base game class or use this one. For any other power, you will need to use this model.

Even if a power is intended to be applied temporarely through various models, every Model should have its own temporary power version to properly show tooltips.

## Wrapper

For ease of use BaseLib provides a wrapper for the `CustomTemporaryPowerModel` called `CustomTemporaryPowerModelWrapper`. It will provide default implementations for most things. You can still manually override anything you want.

The wrapper takes two Types in the class definition:
 - **TModel**: The Model that will apply the power. Information from it will be displayed in the powers tooltip.
 - **TPower**: The Power that will be applied.

```c#
public class FooPower : CustomTemporaryPowerModelWrapper<Anticipate, DexterityPower> {}
```

## CustomTemporaryPowerModel

If you do not want to use the wrapper, you can directly inherit `CustomTemporaryPowerModel`.
The properties shown in the example must be overridden.
```c#
public class FooPower : CustomTemporaryPowerModel
{
    public override AbstractModel OriginModel => ModelDb.Card<Anticipate>();
    public override PowerModel InternallyAppliedPower => ModelDb.Power<StrengthPower>();
    protected override Func<PlayerChoiceContext, Creature, decimal, Creature?, CardModel?, bool, Task> ApplyPowerFunc => PowerCmd.Apply<StrengthPower>;
}
```

## CustomTemporaryPowerModel Documentation

`CustomBigBetaIconPath`, `CustomPackedIconPath` and `CustomBigIconPath` should be overriden to load a custom filepath.<br>
*Wrapper provides default implementations*

`Title`, `Description` and `SmartDescriptionLocKey` should be overriden to point to localization keys<br>
*Wrapper provides default implementations*

`ExtraHoverTips` should be overridden to show information about the power and origin model.<br>
*Wrapper provides default implementation*

`InvertInternalPowerAmount` can be used to invert the applied power amount from the applied temporary power amount.

`CanonicalVars` should not be overriden. If you need to override this, copy the contents of the base implementation into your override.

### Additional Features

`LastForXExtraTurns` provides an option to have the power last for multiple turns.

`UntilEndOfOtherSideTurn` will change the removal of the power from the end of the creatures turn to the end of its enemies turn.

### Noteworthy

`TemporaryPowerTitle` is a localization variable that can be used to insert the `InternallyAppliedPower`'s title into the localization text.<br>
*If you override the localization you won't need this since you know which power will be applied and can just insert the title directly.*

## Examples

#### Focused Strike
```c#
public class FocusedStrikePower : CustomTemporaryPowerModelWrapper<FocusedStrike, FocusPower> {}
```
#### Speed Potion
```c#
public class SpeedPotionPower : CustomTemporaryPowerModelWrapper<SpeedPotion, DexterityPower> {}
```

#### Piercing Wail
```c#
public class PiercingWailPower : CustomTemporaryPowerModelWrapper<PiercingWail, StrengthPower>
{
    protected override bool InvertInternalPowerAmount => true;
}
```

#### Thorns that last for 2 turns
```c#
public class MyTemporaryThornsPower : CustomTemporaryPowerModelWrapper<MyCustomModel, ThornsPower>
{
    protected override int LastForXExtraTurns => 1;
    protected override bool UntilEndOfOtherSideTurn => true;
}
```
