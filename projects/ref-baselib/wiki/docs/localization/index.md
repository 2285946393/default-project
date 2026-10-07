---
title: Localization
nav_order: 6
---

## General Features

Call `DefaultLoc.Set` in your mod's initializer to set the language used as a fallback if the current language is missing. Basegame will default to loading English localization as backup.

```cs
        DefaultLoc.Set(ModId, "zhs");
```

{: .no_toc }