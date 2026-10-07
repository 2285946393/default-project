---
title: Advanced
parent: Mod Configuration
---

# Advanced mod configuration

The content on the parent page is intended to be enough for almost every mod, but you can also build a UI using the building blocks used to implement the automatic UI, or even go bare metal and add plain Godot controls.

If you override `SetupConfigUI` in your `SimpleModConfig` subclass, you can customize the UI further, or even build it entirely from scratch. Calling `base.SetupConfigUI(optionContainer)` is recommended, since it may change in the future.  
As of v3.0.8, SetupConfigUI creates a UI for all static properties, adds a Restore Defaults button, and sets up focus neighbors for controller navigation.

```csharp
public override void SetupConfigUI(Control optionContainer)
{
    // Insert whatever into optionContainer here, and it will be shown in your config UI.
    // Call SetupFocusNeighbors() at the end of SetupConfigUI if you have added or moved controls;
    // that way, controllers will be able to navigate through your UI correctly.

    base.SetupConfigUI(optionContainer);
}
```

## Manual load/save

If you change your properties at runtime (i.e. outside the configuration menu), or need to reload them from disk, you can use:  
`ModConfig.SaveDebounced<MyModConfig>()`  
`ModConfig.Load<MyModConfig>()`  

SaveDebounced uses a 1000 ms debounce delay by default, i.e. if it is called again within 1000 ms, the old request is canceled, and the new replaces it.

## Buttons

Methods (static or instance) marked with `[ConfigButton("ButtonLabel")]` generate buttons in the UI.

**Source code order is preserved** for properties as well as methods, so if you want to place a button between two properties, simply 
define the `[ConfigButton]` method between said properties.

```csharp
// "HelloWorld" is used for the text on the button, while the method name "ExampleButton" is used
// for the row label; both are localized the same way properties are.
[ConfigButton("HelloWorld")] 
public static void ExampleButton(MyModConfig myConfigInstance, NConfigButton clickedButton,
                                 NConfigOptionRow row)
{
    // All parameters are optional, and can be specified in any order with any name.
    // None of them are needed for this example, but they are kept to show the supported types.
    
    MainFile.Logger.Info("Hello, world!");

    // We can update settings here, too
    // ExplosionSize = 100;
    // ExplosionRate = 0.75;
}

[ConfigButton("ClickMe", Color = "#abcdef")]
public void NonStaticButton() 
{
    MainFile.Logger.Info($"Hello from instance {this}");
}
```
<img src="{{ '/images/config-button.jpg' | relative_url }}" alt="Example button" />

## Conditional visibility

You can use the `[ConfigVisibleIf]` attribute to decide when options should be visible,
including creating pseudo-tabs where you can show/hide multiple controls based on the value selected in a Dropdown.  
See the in-IDE documentation when hovering over the attribute for more examples, and information about parameter injection.

```csharp
public enum GameTypeEnum { Standard, Hardcore }

public static GameTypeEnum GameType { get; set; } = GameTypeEnum.Standard;

[ConfigSection("StandardOptions")]
[ConfigVisibleIf(nameof(GameType), GameTypeEnum.Standard)]
public static bool FirstStandardOption { get; set; } = true;

[ConfigVisibleIf(nameof(GameType), GameTypeEnum.Standard)]
[ConfigSlider(0, 20)]
public static int SecondStandardOption { get; set; } = 10;

[ConfigSection("HardcoreOptions")]
[ConfigVisibleIf(nameof(GameType), GameTypeEnum.Hardcore)]
public static bool FirstHardcoreOption { get; set; } = true;

[ConfigVisibleIf(nameof(GameType), GameTypeEnum.Hardcore)]
public static bool SecondHardcoreOption { get; set; } = true;
```

You can also use methods with fully custom logic, including things from outside the mod config.

```csharp
[ConfigSection("HardcoreOptions")]
[ConfigVisibleIf(nameof(ShouldShowHardcoreOptions))]
public static bool FirstHardcoreOption { get; set; } = true;

[ConfigVisibleIf(nameof(ShouldShowHardcoreOptions))]
public static bool SecondHardcoreOption { get; set; } = true;

// static is optional here
public bool ShouldShowHardcoreOptions()
{
    return MyModGlobals.MaxBeatenAscension() >= 10 && GameType == GameTypeEnum.Hardcore;
}
```

Or you can have a "master" visibility toggle that handles everything that isn't always visible, in case that is more useful for your mod:

```csharp

// Parameter can be PropertyInfo or MethodInfo  instead if you don't need to support both properties
// and methods/buttons with one visibility method
public bool ShouldShowOption(MemberInfo memberInfo)
{
    if (GameType == GameTypeEnum.Standard) 
    {
        if (memberInfo is PropertyInfo propInfo)
            return !propInfo.Name.Contains("Hardcore") && ...;
        else if (memberInfo is MethodInfo methodInfo)
            // Handle button visibility
    }
    else if (...)
}
```

This would simply be used as `[ConfigVisibleIf(nameof(ShouldShowOption))]`.

## Custom collapsible sections

You can use `CreateCollapsibleSection` to create custom sections with any content, that are collapsed when clicked.  
This is simple, but it's important to add the nodes to the ContentContainer node, rather than the section itself: otherwise,
the layout will look correct, but the collapse logic will not work.

```csharp
// In an override of SetupConfigUI()
var section = CreateCollapsibleSection("SectionLabel", collapsedByDefault: false);
var content = section.ContentContainer;

content.AddChild(...);
content.AddChild(...);

// "content" is a child of the section, so this is all that's needed
optionContainer.AddChild(section);
```


## Customize controls

You can fetch a control after it has been generated, and modify it.

For example, say your mod has a pool of custom cards, and the player drafts a deck at the start of a run.  
You could add a config option to let the player set the number of custom cards to pick, but the value can't be higher than the number of cards they have access to (assuming no duplicates).

Attributes like `[ConfigSlider]` can only support values known at compile time, so you need to do this manually.

```csharp
// Ensure that the initial max value is AT LEAST the max possible dynamic value, or the user's value will
// get clamped to the slider's initial maximum (100 by default) before you call SetRange!
// The same holds for negative values, as the default minimum is 0.
[ConfigSlider(1, 200)]
public static int DraftPoolSize { get; set; } = 10;

public override void SetupConfigUI(Control optionContainer)
{
    base.SetupConfigUI(optionContainer);

    int totalCustomCardsLoaded = YourMod.GetAllCustomCards().Count;

    // Using % (Godot unique name syntax) is required; if the row is inside a section, it's
    // not a direct child, so GetNode won't find it based on just the name. With % it's found
    // regardless of the nesting.
    var sliderRow = optionContainer.GetNodeOrNull<NConfigOptionRow>($"%{nameof(DraftPoolSize)}");
    if (sliderRow?.SettingControl is NConfigSlider slider)
    {
        // Update the slider limits
        slider.SetRange(1, totalCustomCardsLoaded, 1);
        
        // Make the slider 500 px wide (default is 324 px)
        slider.CustomMinimumSize = new Vector2(500, slider.CustomMinimumSize.Y);
    }
}
```
<img src="{{ '/images/config-wide-slider.jpg' | relative_url }}" alt="Extra wide slider" />

## Adding custom controls

You can insert custom controls before calling `base.SetupConfigUI()` (inserting them above the auto-generated controls), or after (below).  
If you insert custom controls after `base.SetupConfigUI()`, they will end up below the Reset Defaults button.  
You can move it back to the bottom with:

```csharp
var resetContainer = optionContainer.GetNodeOrNull<Control>("ResetDefaultsButtonContainer");
if (resetContainer != null) optionContainer.MoveChild(resetContainer, -1);
SetupFocusNeighbors(optionContainer); // Fix controller focus order
```

This may be reworked in the future, to make it easier to keep the button at the bottom.

You should always call `SetupFocusNeighbors(optionContainer)` if you've added or moved controls, to ensure that they are reachable with a controller. The method connects all focusable controls together in a top-to-bottom chain (with a loop from top → bottom and bottom → top).

## Additional methods

`SimpleModConfig` has `GenerateOptionsForAllProperties`, `AddRestoreDefaultsButton` and `SetupFocusNeighbors` which together make up the entirety of the automatic UI generation. Call `SetupFocusNeighbors` after adding custom controls, to ensure that controllers can navigate in your UI.

`NConfigSlider` has a `SetRange` method, to update the range at runtime.

`NConfigButton` has a `SetColor` method to update the color at runtime.  

`SimpleModConfig` has `Create*Option`, `CreateSectionHeader`, `CreateCollapsibleSection` and `CreateDividerControl` methods to create a single option row, including layout. Most of the time, using auto-generation and customization will be easier, see above.  

`ModConfig` has `CreateRaw*Control` options that create a control with no layout, no label, no hover tip, etc.