---
title: CustomEventModel
parent: Custom Models
---

Example reimplementation of the `ThisOrThat` event.

Override `Acts` to add an event only to specific acts. Otherwise it will be added to shared events. You can also override IsAllowed to control if an event can appear based on the current run state.

Override IsShared as true for events where all players must choose the same options. Required for combat events.

`Option` is a shortcut method to define an `EventOption`.

```c#
public class TestEvent() : CustomEventModel()
{
    // Using in-code localization for the example; you do not need to use this.
    public override List<(string, string)> Localization => LocManager.Instance.Language switch
    { _ => new EventLoc("Test", 
        new EventPageLoc("INITIAL", "Initial page.",
            new EventOptionLoc("PLAIN", "This", "damage for gold"),
            new EventOptionLoc("ORNATE", "That", "Clumsy for relic")),
        new EventPageLoc("PLAIN", "You picked the boring option."),
        new EventPageLoc("ORNATE", "You picked the wrong option.")
        )
    };

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        Option(Plain).ThatDoesDamage(DynamicVars.HpLoss.IntValue),
        Option(Ornate, HoverTipFactory.FromCardWithCardHoverTips<Clumsy>())
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar(6),
        new GoldVar(0),
        new StringVar("Curse", ModelDb.Card<Clumsy>().Title)
    ];

    public override void CalculateVars()
    {
        DynamicVars.Gold.BaseValue = Rng.NextInt(41, 69);
    }
    
    public async Task Plain()
    {
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner!.Creature, DynamicVars.HpLoss.IntValue, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        await PlayerCmd.GainGold(DynamicVars.Gold.IntValue, Owner);
        SetEventFinished(PageDescription("PLAIN"));
    }

    public async Task Ornate()
    {
        await RelicCmd.Obtain(RelicFactory.PullNextRelicFromFront(Owner!).ToMutable(), Owner!);
        await CardPileCmd.AddCurseToDeck<Clumsy>(Owner!);
        SetEventFinished(PageDescription("ORNATE"));
    }

    public override string CustomInitialPortraitPath => ImageHelper.GetImagePath($"events/{ModelDb.Event<ThisOrThat>().Id.Entry.ToLowerInvariant()}.png");
    public override string CustomBackgroundScenePath => SceneHelper.GetScenePath("events/background_scenes/" + ModelDb.Event<ThisOrThat>().Id.Entry.ToLowerInvariant());
}
```