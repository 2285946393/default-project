---
title: Custom Models
nav_order: 2
---

BaseLib provides a set of abstract model classes that inherit the game's base model classes.

These model classes are what define all of the game's content. These models correspond to node classes, such as `NCard` for `CardModel` which handles the visuals and input interactions through Godot, while the model defines all of the class's behavior.

The classes provided by BaseLib include some utilities to make it easier to make modded content, such as using custom paths for assets or automating some parts of their definitions. In addition, BaseLib will automatically prefix the ID of any custom model with its root namespace, greatly reducing the likelihood of multiple mods having models with the same ID. You are not required to use a `CustomModel` class directly for this; you can use `ICustomModel` to mark your model as a custom model for prefixing.

The pages in this section explain what each model class is for, and what additional utilities BaseLib provides.

{: .no_toc }

## General Model Info

Models are tracked in the game's `ModelDb` class. The game automatically loads all model classes at launch.