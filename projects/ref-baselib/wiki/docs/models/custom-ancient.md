---
title: CustomAncientModel
parent: Custom Models
---

`CustomAncientModel` contains more supporting code than most other custom models, making their definition much less similar to basegame ancients.

## Relic Pools

`CustomAncientModel` allows setting up 1, 2, or 3 pools of options. Most basegame ancients have their options separated into different pools, preventing certain similar options from appearing together.

If 3 pools are used, each option is generated from its own pool. With 2 pools, the first two options will use the first pool, and the third option will use the second pool. If only one pool is defined, all options will pull from this pool.

The pools are defined by overriding `MakeOptionPools` and returning a new `OptionPools` object. The individual pools are created using the `MakePool` and `AncientOption` method defined in `CustomAncientModel`.

```c#
    protected override OptionPools MakeOptionPools => new(
        MakePool(
            AncientOption<Astrolabe>(),
            AncientOption<Astrolabe>(),
            AncientOption<SeaGlass>(relicPrep: (glass) => //An example of a relic that requires setup.
            {
                if (Owner == null) return glass;
                var character = Owner.Character;
                var characterModel = Rng.NextItem(Owner.UnlockState.Characters.Where((Func<CharacterModel, bool>) (c => c.Id != character.Id))) ?? character;
                glass.CharacterId = characterModel.Id;
                return glass;
            })
        ),
        MakePool( //Options can be assigned weight to change their appearance rates. Clone is a bit rarer, for example.
            AncientOption<Astrolabe>(weight: 100),
            AncientOption<Astrolabe>(weight: 10),
            AncientOption<TheBoot>(weight: 1)
        ),
        MakePool(
            AncientOption<Astrolabe>(),
            AncientOption<Astrolabe>(),
            AncientOption<Astrolabe>()
        ));
```

You can add spawn conditions to relics by calling `this.AddCustomAncientSpawnCondition` in its constructor. This is used rather than the basegame `IsAllowed` method on relics due to `IsAllowed` receiving no context, and having no basegame uses/implementations (so how it is intended to be used is undetermined). All checks should be performed based on the ancient event passed to the condition function. If checking the player is required, check the event's owner.

## Spawning

`CustomAncientModel` cannot normally spawn in act 1. The only way to change this is by overriding `ShouldForceSpawn` and returning true if the chosen ancient was Neow.

For most ancients, `IsValidForAct` is what should be utilized. Most ancients can only spawn in either act 2 or act 3, not both. Darv is currently the only exception. `CustomAncientModel` defaults to spawning in both act 2 and 3. If you want your ancient to only spawn in a specific act number, a simple check like `public override bool IsValidForAct(ActModel act) => act.ActNumber() == 2;` should be used.

If `ShouldForceSpawn` is used, `IsValidForAct` should most likely be overridden to return false.

## Localization

`CustomAncientModel` is set up to load dialogues automatically if they are set up according to the same format as basegame ancient dialogue.

See [Ancient Dialogues]({% link docs/localization/ancient-dialogue.md %})

Besides dialogues, three entries, `title`, `epithet`, and `talk.firstVisitEver.0-0.ancient` are required.

Having at least one repeatable conversation with `ANY` is recommended to avoid having an interaction with no possible dialogues.

## Assets


# Example

```c#
public class Foo : CustomAncientModel {
    public override bool IsValidForAct(ActModel act) => act.ActNumber() == 2;

    protected override OptionPools MakeOptionPools => new(
        MakePool(
            AncientOption<Astrolabe>(),
            AncientOption<Astrolabe>(),
            AncientOption<SeaGlass>(relicPrep: (glass) => //An example of a relic that requires setup.
            {
                if (Owner == null) return glass;
                var character = Owner.Character;
                var characterModel = Rng.NextItem(Owner.UnlockState.Characters.Where((Func<CharacterModel, bool>) (c => c.Id != character.Id))) ?? character;
                glass.CharacterId = characterModel.Id;
                return glass;
            })
        ),
        MakePool( //Options can be assigned weight to change their appearance rates. Clone is a bit rarer, for example.
            AncientOption<Astrolabe>(weight: 100),
            AncientOption<Astrolabe>(weight: 10),
            AncientOption<TheBoot>(weight: 1)
        ),
        MakePool(
            AncientOption<Astrolabe>(),
            AncientOption<Astrolabe>(),
            AncientOption<Astrolabe>()
        ));

    public override string CustomScenePath => "res://MyMod/ancients/scenes/foo.tscn";
    public override string CustomMapIconPath => "res://MyMod/ancients/images/foo_map.png";
    public override string CustomMapIconOutlinePath => "res://MyMod/ancients/images/foo_map_outline.png";
}
```
