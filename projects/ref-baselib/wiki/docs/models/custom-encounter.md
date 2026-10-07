---
title: CustomEncounterModel
parent: Custom Models
---

Defining an encounter starts with its `RoomType`, which determines where it can spawn, and if it is a `Boss` room, that it also requires a special run history icon.

Override `IsValidForAct` to determine where an encounter can spawn. Filtering to a specific act or act number is recommended.

If you are making a custom act, you can add the encounter to the act either through `IsValidForAct`, or return `false` in `IsValidForAct` and manually add it to the act's `GenerateAllEncounters` method.

Encounters have a list of slots. These should correspond to named nodes in the encounter scene.

The encounter scene is a 1920x1080 Control with Full Rect anchors, and Marker2D children for enemy positions. The names of these markers (corresponding to Slots) can be used with CreatureCmd.Add when spawning additional enemies. The game will place enemies in default positions if an encounter scene is not provided.

`AllPossibleMonsters` should be a simple list of all enemies that appear in the fight. Repeating duplicates is not necessary.

`GenerateMonsters` is used to generate the actual monsters for a fight, and should call `ToMutable` on the monsters generated. Each monster is paired with an optional string key (null to omit) which sets its spawning Slot. If the enemies in the fight are randomized, that should be done in this method using the encounter's `Rng` object.


For `CustomEncounterBackground`, use `BackgroundAssets` if reusing a basegame set of assets, or a new `CustomBackgroundAssets` if using custom file paths.

[See comments in class for additional documentation of methods that can be overridden.](https://github.com/Alchyr/BaseLib-StS2/blob/master/Abstracts/CustomEncounterModel.cs)

```c#

public class HellEncounter : CustomEncounterModel
{
    public HellEncounter() : base(RoomType.Boss)
    {
    }

    public override IReadOnlyList<string> Slots =>
    [
        "queen",
        "amalgam",
        "doormaker",
        "test_subject"
    ];

    public override string? CustomScenePath => "res://mod/scenes/HellEncounter.tscn";

    //Visuals and camera position of combat scene; all optional
    public override BackgroundAssets? CustomEncounterBackground(ActModel parentAct, Rng rng)
    {
        return new BackgroundAssets(ModelDb.Encounter<QueenBoss>().Id.Entry.ToLowerInvariant(), Rng);
    }
    public override string CustomBgm => "event:/music/act3_boss_queen";
    public override float GetCameraScaling() => 0.85f;
    public override Vector2 GetCameraOffset() => Vector2.Down * 60f;

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<TorchHeadAmalgam>().ToMutable(), "amalgam"),
        (ModelDb.Monster<Queen>().ToMutable(), "queen"),
        (ModelDb.Monster<Doormaker>().ToMutable(), "doormaker"),
        (ModelDb.Monster<TestSubject>().ToMutable(), "test_subject")
    ];

    public override IEnumerable<MonsterModel> AllPossibleMonsters => 
    [
        ModelDb.Monster<Queen>(),
        ModelDb.Monster<TorchHeadAmalgam>(),
        ModelDb.Monster<Doormaker>(),
        ModelDb.Monster<TestSubject>()
    ];

    //An image must be set if making a boss. Otherwise, it will attempt to load a boss-specific image using the encounter name from basegame resources and fail.
    public override string? CustomRunHistoryIconOutlinePath => ImageHelper.GetImagePath($"ui/run_history/{ModelDb.Encounter<QueenBoss>().Id.Entry.ToLowerInvariant()}.png");
    public override string? CustomRunHistoryIconPath => ImageHelper.GetImagePath($"ui/run_history/{ModelDb.Encounter<QueenBoss>().Id.Entry.ToLowerInvariant()}_outline.png");

    public override bool IsValidForAct(ActModel act)
    {
        return act.ActNumber() == 3;
    }
}
```