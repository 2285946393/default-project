---
title: CustomOrbModel
parent: Custom Models
---

Orbs are the Defect's core mechanic, but can be applied to any character. `CustomOrbModel` provides hooks for custom icons, sprites, sound effects, and optional inclusion in the random orb pool used by cards like Chaos and Trash to Treasure.

## Icon and Sprite

Override `CustomIconPath` to provide a custom icon used in tooltips and the orb slot UI.

Override `CustomSpritePath` to point to a custom Godot scene for the orb's in-slot visual. The scene must have a [`SpineSprite` node](https://esotericsoftware.com/spine-godot#SpineSprite-Node) named `SpineSkeleton` with an `idle_loop` animation.

Alternatively, override `CreateCustomSprite()` to build the orb visual programmatically. For example by compositing existing orb scenes with color tints and scale adjustments. If `CreateCustomSprite()` returns a non-null value, it takes precedence over `CustomSpritePath`.
```c#
public override Node2D? CreateCustomSprite()
{
    var container = new Node2D();
    string lightningPath = SceneHelper.GetScenePath("orbs/orb_visuals/lightning_orb");
    Node2D lightning = PreloadManager.Cache.GetScene(lightningPath)
        .Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
    new MegaSprite(lightning.GetNode("SpineSkeleton"))
        .GetAnimationState().SetAnimation("idle_loop");
    // change the color and size
    lightning.Modulate = new Color(0.8f, 0.1f, 0.0f, 1.0f);
    lightning.Scale = new Vector2(1.1f, 1.1f);
    container.AddChild(lightning);
    return container;
}
```

## Sound Effects

Override `CustomPassiveSfx`, `CustomEvokeSfx`, and `CustomChannelSfx` to provide sound paths for your orb. The game's audio system passes these strings to an internal GDScript audio proxy. In practice, vanilla orbs use FMOD event paths (e.g. `"event:/sfx/characters/defect/defect_lightning_passive"`), but modders cannot ship FMOD banks.

Leave these as `null` to use the debug audio fallback, which automatically looks for `.mp3` files at `res://debug_audio/{id_lowercase}_passive.mp3`, `res://debug_audio/{id_lowercase}_evoke.mp3`, and `res://debug_audio/{id_lowercase}_channel.mp3`. In practice, most mods utilize existing vanilla orb sounds, or leave audio as `null` and accept the missing resource warning.

Note: Proper mod audio support is actively being worked on in BaseLib.

## Random Orb Pool

By default, custom orbs will not appear from cards like Chaos or Trash to Treasure. Set `IncludeInRandomPool` to `true` to include your orb in the random pool alongside the five vanilla orbs, giving it equal weight.
```c#
public override bool IncludeInRandomPool => true;
```

## Example
```c#
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MyMod;


public sealed class PoisonOrb : CustomOrbModel
{
    public override Color DarkenedColor => new Color("2d6e2d");
    public override string? CustomIconPath => "res://MyMod/images/orbs/poison_orb.png";
    public override bool IncludeInRandomPool => true;

    // Reuse Dark Orb sounds - practical use of overrides
    public override string? CustomPassiveSfx => "event:/sfx/characters/defect/defect_dark_passive";
    public override string? CustomEvokeSfx => "event:/sfx/characters/defect/defect_dark_evoke";
    public override string? CustomChannelSfx => "event:/sfx/characters/defect/defect_dark_channel";

    public override decimal PassiveVal => ModifyOrbValue(3m);
    public override decimal EvokeVal => ModifyOrbValue(6m);

    public override Node2D? CreateCustomSprite()
    {
        var container = new Node2D();
        // back layer: dark orb (green tint)
        string darkPath = SceneHelper.GetScenePath("orbs/orb_visuals/dark_orb");
        Node2D dark = PreloadManager.Cache.GetScene(darkPath)
            .Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
        new MegaSprite(dark.GetNode("SpineSkeleton"))
            .GetAnimationState().SetAnimation("idle_loop");
        dark.Modulate = new Color(0.1f, 0.5f, 0.1f, 1.0f);
        dark.Scale = new Vector2(1.1f, 1.1f);
        container.AddChild(dark);
        // front layer: glass orb (bright green core)
        string glassPath = SceneHelper.GetScenePath("orbs/orb_visuals/glass_orb");
        Node2D glass = PreloadManager.Cache.GetScene(glassPath)
            .Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
        new MegaSprite(glass.GetNode("SpineSkeleton"))
            .GetAnimationState().SetAnimation("idle_loop");
        glass.Modulate = new Color(0.3f, 0.9f, 0.3f, 1.0f);
        container.AddChild(glass);
        return container;
    }

    // Trigger passive at end of turn - standard pattern for all orbs
    public override async Task BeforeTurnEndOrbTrigger(PlayerChoiceContext choiceContext)
        => await Passive(choiceContext, null);
    
    public override async Task Passive(PlayerChoiceContext choiceContext, Creature? target)
    {
        Trigger(); // fires the orb pulse animation - always call this first
        var enemies = CombatState.GetOpponentsOf(Owner.Creature)
            .Where(e => e.IsHittable).ToList();
        if (enemies.Count == 0) return;
        Creature passiveTarget = target ?? Owner.RunState.Rng.CombatTargets.NextItem(enemies)!;
        await PowerCmd.Apply<PoisonPower>(passiveTarget, PassiveVal, Owner.Creature, null);
    }
    
    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext choiceContext)
    {
        var enemies = CombatState.GetOpponentsOf(Owner.Creature)
            .Where(e => e.IsHittable).ToList();
        if (enemies.Count == 0) return enemies;
        Creature target = Owner.RunState.Rng.CombatTargets.NextItem(enemies)!;
        PlayEvokeSfx(); // call before dealing damage so sound plays with the hit
        await CreatureCmd.Damage(choiceContext, new[] { target }, EvokeVal, ValueProp.Unpowered, Owner.Creature);
        return enemies;
    }
}
```

## Important Information from `OrbModel`

The `OrbModel` class in the base game has some quirks worth mentioning.

### Passive and Evoke Values

`PassiveVal` and `EvokeVal` are used by the tooltip system to display the orb's current damage numbers. Always wrap your base values in `ModifyOrbValue()`! This applies Focus scaling automatically, the same way vanilla orbs work.
```c#
public override decimal PassiveVal => ModifyOrbValue(2m);
public override decimal EvokeVal => ModifyOrbValue(4m);
```

### Passive Trigger Timing

There are two hooks for orb passive triggers:

- `BeforeTurnEndOrbTrigger` - fires at the end of your turn, before the flush. This is what all vanilla orbs use.
- `AfterTurnStartOrbTrigger` - fires at the start of your turn, after drawing. Use this if you want your orb to trigger at the beginning of the turn instead.

Most custom orbs should use `BeforeTurnEndOrbTrigger` and delegate to `Passive()`:
```c#
public override async Task BeforeTurnEndOrbTrigger(PlayerChoiceContext choiceContext)
    => await Passive(choiceContext, null);
```

### Trigger Animation

Always call `Trigger()` at the start of your `Passive()` implementation. This fires the orb's pulse animation. Without it the orb will function correctly but won't visually react when its passive activates.
```c#
public override async Task Passive(PlayerChoiceContext choiceContext, Creature? target)
{
    Trigger(); // always call this first
    // ... your passive logic
}
```
