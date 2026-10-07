---
title: In-Code Localization
parent: Localization
---

`BaseLib` offers the option of defining localization in code by implementing `ILocalizationProvider` in a model class.

It is important to note that while this may be more convenient for the developer, it will make the work of translators more difficult.

All custom model classes already implement `ILocalizationProvider`. It can be used by overriding the `Localization` method. This localization is loaded after the model database is initialized, and is not dynamically updated (may be changed in the future).

BaseLib provides utility record classes to quickly define localization for specific types.

This localization will be added to a localization table based on the model's type, but this table can be set by defining `LocTable`.

```cs
    public override List<(string, string)> Localization => new PowerLoc(
        "Power Name", 
        "Description", 
        "Smart Description");
```

```cs
    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    { 
        "zhs" => new CardLoc("zhs", "zhs description"), 
        _ => new CardLoc("default", "description") };
```