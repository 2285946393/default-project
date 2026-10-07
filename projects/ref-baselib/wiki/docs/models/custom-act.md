---
title: CustomActModel
parent: Custom Models
---

[Stub page; see comments](https://github.com/Alchyr/BaseLib-StS2/blob/master/Abstracts/CustomActModel.cs)


```cs
//Minimal implementation of CustomActModel.
//An act 3 that mostly just mimics basegame act 3.
//Encounters, events, map textures, and rest site scene are required.
//All other overrides are optional for custom visuals/colors/music.
public class BareMinimumActWithTooManyRooms() : CustomActModel(3)
{
    public override IEnumerable<EncounterModel> GenerateAllEncounters() => ModelDb.Act<Glory>().GenerateAllEncounters();

    public override IEnumerable<EventModel> AllEvents => ModelDb.Act<Glory>().AllEvents;

    protected override string CustomMapTopBgPath => ModelDb.Act<Glory>().MapTopBgPath;
    protected override string CustomMapMidBgPath => ModelDb.Act<Glory>().MapMidBgPath;
    protected override string CustomMapBotBgPath => ModelDb.Act<Glory>().MapBotBgPath;
    protected override string CustomRestSiteBackgroundPath => ModelDb.Act<Glory>().RestSiteBackgroundPath;

    //This one is not required.
    protected override int BaseNumberOfRooms => 999;
}
```