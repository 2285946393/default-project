---
title: Mod Interop
parent: Utilities
---

`ModInterop` allows mods to utilize code from other mods without using them as a hard dependency.

Note that this is effectively a wrapper around reflection best used in some edge cases where some more complex use of a class in another mod is desired (while avoiding a hard dependency).

It can also be used as a way of accessing private fields/properties/classes.

## Usage

First, define a class with the annotation `[ModInterop]`.

```c#
[ModInterop("OtherModId")]
public static class Interop {
    //Being static is optional, but this class in general should never be instantiated.

}
```

ModInterop has three main functions:
* Calling methods
* Setting/getting fields/properties
* Mimicking classes

Methods, fields, and properties need the name of the type that they are defined in and the name of the member. The type name can be set directly in the ModInterop attribute, or individually on each member. The member name can simply use the defined method/property name or be set by an `[InteropTarget]` attribute.

Fields and properties are both accessed by defining properties. Methods and properties defined directly in the interop class must be static.

ModInterop generation occurs after all mod initialization and before the ModelDb is initalized.

```c#
[ModInterop("OtherModId", "OtherMod.Namespace.Here")]
public static class Interop {
    public static void Test(int num)
    {
        //Will mimic method OtherMod.Namespace.Here.Test
    }

    [InteropTarget("MethodName")]
    public static void Annotated(object obj)
    {
        //Will mimic OtherMod.Namespace.Here.MethodName
        //object parameters will be treated as wildcards in case a method uses a parameter of a type you don't have access to.
    }

    public static int Number { get; set; } //Access property or field OtherMod.Namespace.Here.Number

    [InteropTarget("OtherMod.OtherNamespace", "SecretId")]
    public static string Id { get; set; } //OtherMod.OtherNamespace.SecretId

    [InteropTarget("OtherMod.OtherNamespace.SomeClass")]
    public class SomeClass : InteropClassWrapper {
        public class SomeClass(string id, int val) {
            //Can be used to construct a new instance of OtherMod.OtherNamespace.SomeClass if it has a matching constructor
        }

        //You don't need to define all properties/methods in SomeClass, only the ones you want to access.
        public int SpecialValue() {
            return 0;
        }

        //Can be used similarly to reflection to generate a static getter method given an instance.
        public static int SpecialValue(object instance) {
            return 0;
        }

        //The constructed instance of the actual class can be accessed with this.Value, but it is stored as an object.
    }
}
```