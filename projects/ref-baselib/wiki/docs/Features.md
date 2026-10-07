---
title: Features List
nav_order: 1
---

## List of Notable Features and Things That Still Need Documentation

* [Declarative (or custom) mod configuration]({% link docs/utilities/config.md %})
* [CustomModel classes for content additions]({% link docs/models/index.md %})
* Automatic ID prefixing of models that inherit ICustomModel (or any CustomModel class)
* [Custom Enums (and keywords)]({% link docs/utilities/enums.md %})
* [GeneratedNodePool - Add nodes generated through code to NodePool]({% link docs/utilities/pooling.md %})
* [SpireField - Effectively add a field to an existing class]({% link docs/utilities/spirefield.md %})
* [AddedNode - Variant of SpireField for adding nodes to existing scenes]({% link docs/scenes/add-nodes.md %})
* Easy addition of extra UI for cards, relics, and potions (ICustomUiModel, or use AddedNode)
* [Use of Godot AnimationPlayer/AnimationTree/AnimationPlayer2D for creature visuals](% link docs/scenes/creature-visuals.md %)

## Smaller Things That Won't Get A Full Page

* `DescriptionOverrides.CustomizeDescription += delegate` (global description modifiers)
* `RelicImageOverridePatch.AddOverride<BurningBlood>(new("relic.png".BigRelicImagePath(), "relic.png".RelicImagePath(), "relic_outline.png".RelicImagePath()));` (relic image overrides, can add condition)
* Add scrolling when too many characters for normal character selection

## List of Things that BaseLib Does That Aren't Exactly Features But Should Be Mentioned

* Properties marked with `[SavedProperty]` are registered with the game's `SavedPropertiesTypeCache` automatically
* Missing localization will log an error rather than crashing
* Custom character card pools will have an option added to the compendium
* Debuffs self-applied during the player's turn involving CustomModel classes will not skip their first duration tick
* Compatibility patches to prevent crashes when using a custom character
* CommonActions class with some shortenings of commands commonly used by cards
