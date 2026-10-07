---
title: Calculated Vars
parent: Utilities
---

Calculated variables are useful, but basegame calculated variables have a few limitations.

Due to having fixed names, you can only have a limited set. They will all look for the same base value, and they only work on cards. Defining them is somewhat more complicated than necessary. BaseLib provides utilities to help with these.

### Quick Define

In a card, you can call `MakeCalculatedVar`/`MakeCalculatedDamage`/`MakeCalculatedBlock` to quickly define all three variables necessary for a calculated variable.

```cs
    //Defining Body Slam's calculated damage variable
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        ..MakeCalculatedDamage(0, (card, target) => card.Owner.Creature.Block)];
```

`..` adds multiple elements to a collection. These methods have various optional parameters.

By default, the multiplier will be set to `1`, meaning the bonus calculation is applied as-is.

`MakeCalculatedVar` will require a name parameter, while `MakeCalculatedDamage` and `MakeCalculatedBlock` can use one optionally.

When a name is provided, a `CustomCalculatedVar` class will be used instead of the basegame classes.

### Custom Calculated Var

There are three custom calculated var classes. `CustomCalculatedDamageVar`, `CustomCalculatedBlockVar`, and `CustomCalculatedVar`.

They behave identically to basegame calculated variables, but can use a custom name. Given a name of "Variable", they will look for dynamic variables named "VariableExtra" and "VariableBase".

In addition, they can be used in relics and powers. There is currently no shortcut method provided for these model types.
