---
title: Merchant Character Visuals
parent: Scenes
---

## NMerchantCharacter

This is the script for a character's visuals while they are at a merchant. There are almost no requirements for this scene.

The path for this scene can be set by overriding `CustomMerchantAnimPath` in the character class. If this scene is not an `NMerchantCharacter`, BaseLib will convert it into one.

BaseLib supports the same animation types as for regular creature visuals, using an `AnimationTree`/`AnimationPlayer` or `AnimationPlayer2D`.

The game will attempt to play the `relaxed_loop` (alternative allowed names for godot animations: `idle`, `Idle`) animation. You can make an animation with this name, or just have an idle animation be the default.

The only other animation that may be called for is `die`.


## Spine

First child node must be the Spine node.