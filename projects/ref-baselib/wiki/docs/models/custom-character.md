---
title: CustomCharacterModel
parent: Custom Models
---

BaseLib assists in setting up pools and visuals for a character.

`PlaceholderCharacterModel` will utilize the assets of the basegame character determined by the `PlaceholderID` method, defaulting to `ironclad`.

## Pools (Stub)

`CustomCardPoolModel`

`CustomRelicPoolModel`

`CustomPotionPoolModel`

## Visuals (Stub)

[Character (combat) Visuals]({% link docs/scenes/creature-visuals.md %})

Rest Site Visuals

Merchant Visuals

Energy Counter

Yummy Cookie

```cs
    public override RelicIconData CustomYummyCookie => new(
        "relic.png".BigRelicImagePath(),
        "relic.png".RelicImagePath(), 
        "relic_outline.png".RelicImagePath());
```

## Extra

`HideFromVanillaCharacterSelect` - Hides a character from the standard character select screen.

`AllowInVanillaRandomCharacterSelect` - defaults to inverse of `HideFromVanillaCharacterSelect`. Removes a character from random selection.

`SetupCustomAnimationStates` - Used exclusively for Spine animations.