---
title: Energy Counter
parent: Scenes
---

## NEnergyCounter

This is the script (effectively the base type) that all energy counters must be. However, when creating a scene through Godot's editor, scripts from other assemblies are not accessible. There are a few options to work around this.

* Use `NodeFactory<NEnergyCounter>.CreateFromScene` to convert a scene that isn't using `NEnergyCounter` as its script into an `NEnergyCounter` instance. `CustomCharacterModel` will do this by default if the scene path provided for its energy counter does not inherit from `NEnergyCounter`.
* Define a class inheriting from `NEnergyCounter` marked with the `[GlobalClass]` attribute and use it as the script for your scene. You can add additional functionality in this class as well if you wish.
* Define the scene manually by editing a .tscn file in text, which allows you to set its script to `NEnergyCounter`. You will not be able to edit this scene in the Godot editor.

## Scene Setup

An `NEnergyCounter` follows a specific setup and has required named nodes. If you create a class inheriting `NEnergyCounter` and use it as the script, you will need to match this setup exactly.

The root node should be a Control. Its size is expected to be 128x128, with a pivot offset of (64, 64). It should have the following children.

| Name              | Unique Name | Details                                                                                                    | Optional |
|-------------------|:-----------:|------------------------------------------------------------------------------------------------------------|:--------:|
| EnergyVfxBack     |     Yes     | Node2D. Should be or have GpuParticles2D as children.                                                      |    No    |
| Layers            |     Yes     | Control. Static images like background and overlay of energy counter.                                      |    No    |
| RotationLayers    |     Yes     | Control. Child of Layers. Images that rotate and are usually between textures of Layers.                   |    No    |
| EnergyVfxFront    |     Yes     | Node2D. Should be or have GpuParticles2D as children.                                                      |    No    |
| Label             |     No      | MegaLabel. If converting scene, just use Label. Displays current energy.                                   |    No    |

If your scene is converted by `NodeFactory` the requirements are slightly different. EnergyVfxBack, EnergyVfxFront, RotationLayers, and Label are optional. A default label will be generated and the others will be left blank. You do not have to manually set nodes to have unique names, as long as their name is correct.


# Setup in Godot For Conversion

First, create a new scene with a `Control` as the root node. You are recommended to rename it. In the right panel, under Layout -> Transform, set its size to 128x128 and pivot anchor to (64, 64). Then, push ctrl+L or click the lock button on the top bar to prevent editing this node.

Add a `Node2D` as a child of the root node and name it `EnergyVfxBack`. Set its position to (64, 64).

Add a `Control` as a child of the root node and name it `Layers`. Set its Layout Mode to `Anchors` and then set its Anchors Preset to `Full Rect`. Under `Mouse` set its Filter to `Ignore`. Add a `Control` as a child of `Layers` and name it `RotationLayers`. Set up this `Control` the same way.

Add a `Node2D` as a child of the root node and name it `EnergyVfxFront`. Set its position to (64, 64).

Add a `Label` as a child of the root node. Set its Layout Mode to `Anchors` and Anchors Preset to `Full Rect`. Set its Horizontal and Vertical alignment for text to `Center`.

Everything past this point does not need to be followed strictly; it can be set up in any way to achieve the appearance you want.

Now for visuals, add `TextureRect` nodes as children of `Layers` and `RotationLayers`. You will generally want these to have their Expand Mode as `Ignore Size` and Stretch Mode as `Keep Aspect`, with the Layout Mode being `Anchors` and the preset being `Full Rect`. For rotation layers you should set their `Pivot Offset` to their center, (64, 64).

Adjust the text's appearance by setting its Theme Overrides. If the font is left as default, the basegame font will be used at runtime. To mimic basegame energy counter font appearance, set theme Overrides -> Constants -> Shadow Offset X to 3, Shadow Offset Y to 2, Outline Size to 16, and Shadow Outline Size to 16. Due to how `NEnergyCounter` adjusts text colors based on current energy, you cannot customize the font color through Godot. You can only set the font's outline color through the `EnergyLabelOutlineColor` method override in the player class.