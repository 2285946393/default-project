---
title: Audio
parent: Utilities
---

Manual:

```cs
//Define:
public static class ModSounds
{
    public static readonly ModSound Sound = new("res://Mod/audio/sound.ogg");
}

//Play:
ModSounds.Sound.Play(); //See optional params of play methods.
```

Using AutoModAudio:

```cs
//Define:
public class MainFile
{
    public const string ModId = "Mod";
    
    public static readonly AutoModAudio Audio = new AutoModAudio($"res://{ModId}/audio");


//Play:
MainFile.Audio.PlaySfx("sound.ogg");
```

BaseLib will also play sounds when a resource path is provided to the basegame FMOD event player.

```cs
    //In a character
    public override string CharacterTransitionSfx => "res://Mod/audio/sound.ogg";
    public override string CharacterSelectSfx => "res://Mod/audio/sound.ogg";
```


Playing sounds directly without defining them first:

```cs
ModAudio.PlaySound("res://Mod/audio/sound.ogg");
```