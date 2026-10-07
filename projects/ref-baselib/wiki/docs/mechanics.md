---
title: Hooks and Mechanics
nav_order: 3
---

Hooks and mechanics added by BaseLib that can be used by any mod.

## Hooks

* `IHealAmountModifier`: `ModifyHealAdditive` and `ModifyHealMultiplicative`
* `IMaxHandSizeModifier`: `ModifyMaxHandSize` and `ModifyMaxHandSizeLate`
* `IHealthBarForecastSource`: Supported directly by powers; other implementations must be registered with `HealthBarForecastRegistry`


## Keywords

Add these to your card's CanonicalKeywords for them to function.

| Name                  | Description                                                                                                                    |
|:----------------------|:-------------------------------------------------------------------------------------------------------------------------------|
| BaseLibKeywords.Purge | Cards with this keyword will be removed from your deck when played.                                                            |


## Dynamic Variables

Add these to your card's CanonicalVariables and to their card text.

| Name         | Card Text                                    | Description                                                                              |
|:-------------|:---------------------------------------------|:-----------------------------------------------------------------------------------------|
| Persist      | [gold]Persist[/gold] {Persist:diff()}.       | Card returns to hand when played a certain number of times each turn.                    |
| Exhaustive   | [gold]Exhaustive[/gold] {Exhaustive:diff()}. | Card Exhausts after a certain number of uses each combat.                                |
| Refund       | [gold]Refund[/gold] {Refund:diff()}.         | Card returns some of its energy cost after play. Will not refund more than energy spent. |


## Targeting

Alternative values that can be used as a card's target type. These targeting types are all defined in `CustomTargetType`.

`Everyone`, `Anyone`, `AllAttackingEnemies`, `AnyAttackingEnemy`, `AllBlockingEnemies`, `AnyBlockingEnemy`, `AllNonBlockingEnemies`, `AnyNonBlockingEnemy`, `AllHighestHpEnemies`, `AllLowestHpEnemies`, `AnyFullLifeEnemy`, `AllFullLifeEnemies`

Single target types will pass the target to the card play's target. To get the targets for a multi-targeting type, use the extension method `this.GetTargets()`.

For an attack command, use `TargetingFiltered`, or `CommonActions.CardAttack` which will handle it for you.
```cs
await CommonActions.CardAttack(this, play).Execute(choiceContext);
await new AttackCommand(DynamicVars.Damage.BaseValue)
    .TargetingFiltered(this.GetTargets())
    ...
    .Execute(choiceContext);
```
