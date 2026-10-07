---
title: Ancient Dialogue
parent: Localization
---

`BaseLib` is set up to automatically load dialogues for custom ancients, as well as to add dialogues defined for any ancient and character to that ancient's dialogue collection.

Custom characters are required to define an architect dialogue, as otherwise they will not be able to finish a run.

While interactions between basegame characters and ancients can be defined, due to dialogue indices being checked starting from index 0 this will result in overriding basegame localization, even if you only want to add new interactions. This may be properly supported in the future, but is low priority.

## Dialogue Format

```json
  "DARV.talk.IRONCLAD.0-0.ancient": "The flaming warrior has returned!\nI’ve got just the thing for ya!",
  "DARV.talk.IRONCLAD.1-0r.ancient": "Good to see ya still kickin’ and bashin’ em heads in!",
  "DARV.talk.IRONCLAD.2-0.ancient": "I’m still holding on to that heavy blade of yours... but I can’t find no one to restore it.",
  "DARV.talk.IRONCLAD.2-0.next": "Continue",
  "DARV.talk.IRONCLAD.2-1.char": "[i][font_size=22]The Ironclad stares at Darv.[/font_size][/i]",
  "DARV.talk.IRONCLAD.2-1.next": "Continue",
  "DARV.talk.IRONCLAD.2-2.ancient": "Maybe the Architect knows somethin’.",
```

This sample contains 3 dialogues. Each entry starts with the ancient's ID, followed by `talk`, then the character's ID. A character ID of `ANY` means a line can be used for any character.

The first number is the dialogue index, starting from 0. Dialogue indices must be consecutive. The second number is the index of the line within that dialogue.

The lines end with `ancient` or `char` to determine who is speaking, or `next` for the text displayed on the button to continue the conversation. There should be one speaker and one `next` entry per line, besides the last one which does not need a `next` entry.

Dialogues have a visit index. For most ancients, dialogue 0 is used for the first visit, dialogue 1 is for the second visit, and dialogue 2 is for the 5th visit. For the architect, all dialogues are consecutive. BaseLib will follow this pattern when loading dialogues. If you go above 2, each dialogue after that will require 3 additional visits (remaining consecutive for the architect).

These dialogues are guaranteed to be used on the visit that matches their visit index.

Note the `r` on the second line `1-0r`. This marks this line as repeating, meaning it will be randomly reused after its initial guaranteed use. On any visit to an ancient without a guaranteed visit line, a random line is chosen from repeatable lines in the `ANY` category and already seen repeatable character specific lines.

### Format Additions

The sfx for any line can be set by adding an entry with the same key with `.sfx` added.

```json
  "NEOW.talk.MYMOD-MYCHAR.0-0.ancient": "[sine]hey[/sine]",
  "NEOW.talk.MYMOD-MYCHAR.0-0.ancient.sfx": "event:/sfx/npcs/neow/neow_welcome"
```

A dialogue's visit index can be set to a specific value using a `-visit` after the dialogue index. All following dialogues will continue following the normal pattern, meaning if you set the index to 100 the following dialogue will have an index of 103.

```json  
  "DARV.talk.MYMOD_MYCHAR.3-0.ancient": "This is the 100th time I've seen you?",
  "DARV.talk.MYMOD_MYCHAR.3-visit": "100",`
```

## Architect Example

The Architect's dialogue is generally the same, with a few things to note.

* There should be multiple repeating conversations to avoid having the same line appear constantly. Make sure to have at least one.
* Due to it being relatively predictable when these will occur, some characters have continuity between their first few dialogues with the architect (these are generally not marked as repeating).
* The `-attack` entry can be used to determine who will perform an attack animation. Possible values are `None`, `Player`, `Architect`, and `Both`. If not used it will default to `Architect`. How the ending scene occurs if `None` or just `Player` is used has not been tested.

```json
  "THE_ARCHITECT.talk.MYMOD-MYCHAR.0-0r.char": "I kill you",
  "THE_ARCHITECT.talk.MYMOD-MYCHAR.0-0r.next": "Continue",
  "THE_ARCHITECT.talk.MYMOD-MYCHAR.0-1r.ancient": "No",
  "THE_ARCHITECT.talk.MYMOD-MYCHAR.0-attack": "Both"
```