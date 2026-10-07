---
title: Creature Visuals
parent: Scenes
---

## NCreatureVisuals

This is the script (effectively the base type) that all creature visuals must be. However, when creating a scene through Godot's editor, scripts from other assemblies are not accessible. There are a few options to work around this.

* Use `NodeFactory<NCreatureVisuals>.CreateFromScene` to convert a scene that isn't using `NCreatureVisuals` as its script into an `NCreatureVisuals` instance. `CustomCharacterModel` will do this by default if the scene path provided for its visuals does not inherit from `NCreatureVisuals`.
* Use `NodeFactory<NCreatureVisuals>.CreateFromResource` to convert a `.png` or other common image format into an `NCreatureVisuals` instance. If using this for a character, override `CreateCustomVisuals` and return this.
```cs
    public override NCreatureVisuals CreateCustomVisuals()
    {
        return NodeFactory<NCreatureVisuals>.CreateFromResource("res://Mod/images/character/image.png");
    }
```
* Define a class inheriting from `NCreatureVisuals` marked with the `[GlobalClass]` attribute and use it as the script for your scene. You can add additional functionality in this class as well if you wish.
* Define the visuals scene manually by editing a .tscn file in text, which allows you to set its script to `NCreatureVisuals`. You will not be able to edit this scene in the Godot editor.

## Scene Setup

An `NCreatureVisuals` follows a specific setup and has required named nodes. If you create a class inheriting `NCreatureVisuals` and use it as the script, you will need to match this setup exactly.

The root node should be a Node2D, with the following children.

| Name              | Unique Name | Details                                                                                                    | Optional |
|-------------------|:-----------:|------------------------------------------------------------------------------------------------------------|:--------:|
| Visuals           |     Yes     | Any Node2D. Should contain all visuals.                                                                    |    No    |
| PhobiaModeVisuals |     Yes     | Any Node2D. Alternative visuals if phobia mode is enabled.                                                 |    Yes   |
| Bounds            |     Yes     | Control. Hitbox size.                                                                                      |    No    |
| IntentPos         |     Yes     | Marker2D. Position of intent, usually around top of bounds.                                                |    No    |
| CenterPos         |     Yes     | Marker2D. Used as vfx spawn position. Should be approximately at visual center of character.               |    No    |
| OrbPos            |     Yes     | Marker2D. Central point of orbs, generally similar to intent position. If not provided, IntentPos is used. |    Yes   |
| TalkPos           |     Yes     | Marker2D. Origin of speech bubbles. If not provided an approximate position is chosen based on Bounds.     |    Yes   |

If your scene is converted by `NodeFactory` the requirements are slightly different. Only the `Visuals` node is required, but also including the `Bounds` node is strongly recommended. The other positions will be automatically determined based on `Bounds` if not provided.

## From Static Image

```c#
    public override NCreatureVisuals? CreateCustomVisuals()
    {
        return NodeFactory<NCreatureVisuals>.CreateFromResource("res://Path/To/Your/Image.png");
    }
```

This will first create a basic `NCreatureVisuals` node with a `Visuals` node containing the provided resource as a `Texture2D`, and a `Bounds` node using the size of the image. This will then be passed to the "from scene" factory to add the other necessary nodes.

## Godot Animation

BaseLib supports automatic use of animations defined using either an `AnimationPlayer`, `AnimationPlayer2D`, or `AnimationTree` connected to an `AnimationPlayer` node. The animation names that will automatically be played are the const animation names defined in `CreatureAnimator` or this list of alternative names: "idle", "attack", "cast", "hurt", "die", or the lowercase version of one of the `CreatureAnimator` const values. 

BaseLib will pass the signals from the base game's attempts to play animations to the relevant animation player. For your animations to automatically transition back to idle after played (other than "die") you can edit the animation transitions of the `AnimationPlayer`, or set it up in an `AnimationTree` node. If using an `AnimationTree`, its "Tree Root" will need to to be an `AnimationNodeStateMachine`. When setting up transitions in an `AnimationNodeStateMachine`, you will likely need to set the switch mode of each transition to "At End".

## Spine
