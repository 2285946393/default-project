---
title: Dynamic Variable Tooltips
parent: Localization
---

BaseLib supports attaching localization to a dynamic variable, specifically adding a tooltip for any card that has that dynamic variable. The tooltip's localization should be in `static_hover_tips.json`, with the localization keys being `MODNAME-VAR_NAME.title` and `MODNAME-VAR_NAME.description`.

Adding the tooltip is done by calling `WithTooltip` on the `DynamicVar` instance, which can be done in the constructor of a custom `DynamicVariable` class or by calling it on the variable when defining a card's canonical variables.

```c#
namespace BaseLib.Cards.Variables;

public class PersistVar : DynamicVar
{
    public const string Key = "Persist";

    public PersistVar(decimal persistCount) : base(Key, persistCount)
    {
        this.WithTooltip();
        //Localization key BASELIB-PERSIST
    }
```

```c#
namespace BaseLib.Cards;

//When using DynamicVar directly a prefix cannot be determined, and so there will not be one.
public class ExampleOne : CustomCard(args)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [ new DynamicVar("Name", 6).WithTooltip() ];
    //Localization key NAME

public class ExampleTwo : CustomCard(args)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [ new DynamicVar("Name", 6).WithTooltip("LocKey") ];
    //Localization key LocKey
```

## Example Localization

```json
  "BASELIB-PERSIST.title": "Persist",
  "BASELIB-PERSIST.description": "This card returns to your hand the first {Persist:cond:>1?[blue]{Persist}[/blue] |}{Persist:plural:time|times} it is played each turn.",
```

It is important to note that tooltips generated this way will ONLY have access to their own variable. So in this case, the Persist variable is the only one that can be used inside this tooltip.