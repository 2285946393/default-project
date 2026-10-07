---
title: Simplified Localization
parent: Localization
---

Mods can use simplified localization shortcuts by either opting their entire mod's localization in or by starting a localization entry with `#`. If you want to start a description with `#`, use `##` which will disable SimpleLoc and leave just one `#`.

To opt-in your mod's entire localization, you can call `SimpleLoc.EnableSimpleLoc(ModId);` in your mod's initializer, but this is not *recommended*. It will result in many strings being processed unnecessarily.

## Formatting

`!Var!` will be replaced with `{Var:diff()}`.

`@Var@` will be replaced with `{Var:inverseDiff()}`.

Certain names have shortened versions that can be used.

```c#
        { "D", "Damage" },
        { "CD", "CalculatedDamage" },
        { "B", "Block" },
        { "CB", "CalculatedBlock" },
        { "C", "Cards" },
        { "E", "Energy" },
        { "H", "Heal" }
```

`*Word` will be replaced with `[gold]Word[/gold]`.
Highlighting will end either on whitespace or end of "word" (symbol). You can surround text like `*Wo*rd` -> `[gold]Wo[/gold]rd` to explicitly control what is highlighted.

`(plural)` will be displayed if the most recently referenced variable is plural.
e.g. `Draw !C! card(s).` will become `Draw {Cards:diff()} card{Cards:plural:|s}.`

`-text-` will be removed when a card is upgraded. `+text+` will be added when a card is upgraded. Using them together like `-old-+new+` will generate a combined statement.

`[EEE]` will be replaced with a fixed number of energy icons based on the number of `E`. `[E?]` will be energy icons based on the `Energy` variable.