---
title: Ancient Upgrades
parent: Utilities
---

To set up an upgrade for your starter relic, override `GetUpgradeReplacement` in your starter relic class.

```cs
    public override RelicModel GetUpgradeReplacement() => ModelDb.Relic<BetterStarter>();
```

To set up a starter card upgrade, implement `ITranscendenceCard` in the starter card that will be upgraded.

```cs
public class StarterCard : MyModCard, ITranscendenceCard
{
    public CardModel GetTranscendenceTransformedCard() => ModelDb.Card<UpgradedStarterCard>();


```

Darv's Dusty Tome relic will default to giving a random ancient rarity card in your character's card pool that is not an upgrade from Transcendence.

If you want to change this behavior (using additional ancient rarity cards for other purposes, giving a non-ancient rarity card or a card from a different pool), implement `ITomeCard` on the card(s) you want the Tome to give. One will be chosen at random whenever Dusty Tome is spawned.

```cs
    /*
    If using the character mod template, including the Character namespace will likely be necessary
    due to overlapping naming.
    */
    CharacterModel TomeCharacter => ModelDb.Character<Character.YourCharacter>();
```