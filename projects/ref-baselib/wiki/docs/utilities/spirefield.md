---
title: SpireField
parent: Utilities
---

SpireField is a simple way for mods to effectively add a new field to a preexisting class in the game.

To use it, define a SpireField like so:

`public static readonly SpireField<TargetType, FieldType> MyField = new(()=>default value);`

This field can be placed in any class; you are recommended to place it somewhere related to the field's purpose.

Getting and setting the field can be done like so, given the following example:

```c#
public class SpecialNumberThing {
    public static readonly SpireField<CardModel, int> SpecialNumber = new(() => 0); //Default value of 0
}

...
CardModel model = ModelDb.Card<Bash>();
int val = SpecialNumberThing.SpecialNumber.Get(model);

SpecialNumberThing.SpecialNumber.Set(model, 5);
```

You can also get or set the value using an indexer.

```c#
CardModel model = ModelDb.Card<Bash>();
int val = SpecialNumberThing.SpecialNumber[model];

SpecialNumberThing.SpecialNumber[model] = 5;
```

### Common Cases

For storing per-player, per-combat data (like Defect's orbs), a `SpireField<PlayerCombatState, ?>` is suggested. The `CharacterModel` is not unique between players using the same character, and while `Player` is unique between characters, using `PlayerCombatState` avoids having to reset data manually between combats.

For persistent, per-player data, a `SavedSpireField<Player, ?>` is recommended. See the following section for more information about `SavedSpireField`.

## SavedSpireField

SavedSpireField is a class inheriting from SpireField with the additional functionality of saving and loading its value when attached to a model. Note this will only work for types that the base game saves. Currently, it supports any classes inherited from `CardModel`, `RelicModel`, `PotionModel`, `EnchantmentModel`, `Player`, `Reward`, or `IRunState`.

It is used almost identically to a normal SpireField; the only difference is that you have to provide a name which will be used for the save data. Try to ensure that the name will be something unique to your mod to avoid conflicts.

You can save any type of value in a SavedSpireField, but if it not one of the following types:

- `int`
- `bool`
- `string`
- `int[]`
- `ModelId`
- `SerializableCard`
- `SerializableCard[]`
- `List<SerializableCard>`

You will need to manually register the type you are saving by calling `ExtendedSaveTypes.RegisterAdditionalSaveType` in your mod's initializer. There are convenience methods that handle a large portion of it. If saving a List or Dictionary, use `ExtendedSaveTypes.RegisterListSaveType`/`RegisterDictionarySaveType.RegisterDictionarySaveType`. For other classes, call `ExtendedSaveTypes.RegisterObjectSaveType`. You will need to register each property/field of the object that needs to be saved by passing `ExtendedSaveTypes.PropertyFunc`/`ExtendedSaveTypes.FieldFunc` to the method.

```cs
//Example save type
public class BlahCard : CustomCardModel
{
    //int saved type will work without special registration.
    public static readonly SavedSpireField<BlahCard, int> SpecialValue = new(() => 100, "my_mod_special_value");

    //custom saved type will require registration.
    public static readonly SavedSpireField<BlahCard, TestSaveType> TestBlah = new(() => new()
    {
        Value = "Default Value"
    }, "TestBlahSaveName");

    //If the saved type is not IPacketSerializable, the Serializer and Deserializer properties of the SavedSpireField must be set.
    public class TestSaveType() : IPacketSerializable
    {
        public string Value { get; set; } = "Test";

        public void Serialize(PacketWriter writer)
        {
            writer.WriteString(Value);
        }

        public void Deserialize(PacketReader reader)
        {
            Value = reader.ReadString();
        }
    }

//In mod initializer
ExtendedSaveTypes.RegisterObjectSaveType<BlahCard.TestSaveType>(
    ExtendedSaveTypes.PropertyFunc<BlahCard.TestSaveType, string>(nameof(BlahCard.TestSaveType.Value)));
```

### Technical

SpireField works by wrapping a ConditionalWeakTable. A ConditionalWeakTable can be used directly for similar results, but SpireField provides some utility to make it simpler to use, such as having a default value.