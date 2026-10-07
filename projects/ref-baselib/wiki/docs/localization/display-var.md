---
title: DisplayVar
parent: Localization
---

`DisplayVar` is a `DynamicVar` class you can use to add arbitrary processed text to the description of any model that supports dynamic variables.

```cs
//A variable that would allow you to show 10x another variable's value in description.
new DisplayVar<ModelType>("key", (model) => (model.GetDynamicVar("Something").IntValue * 10).ToString())
```