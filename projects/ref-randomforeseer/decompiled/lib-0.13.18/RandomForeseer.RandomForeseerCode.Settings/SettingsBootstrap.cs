using System;
using System.Runtime.CompilerServices;
using RandomForeseer.RandomForeseerCode.Data;
using RandomForeseer.RandomForeseerCode.Debug;
using RandomForeseer.RandomForeseerCode.Localization;
using STS2RitsuLib;
using STS2RitsuLib.Settings;

namespace RandomForeseer.RandomForeseerCode.Settings;

internal static class SettingsBootstrap
{
	[SpecialName]
	public sealed class _003CG_003E_0024B660017A57D16C65E0C1311648B51E89
	{
		[SpecialName]
		public static class _003CM_003E_002407F5FBE0E78EAC9F936744488E50352C
		{
		}

		[ExtensionMarker("<M>$07F5FBE0E78EAC9F936744488E50352C")]
		private ModSettingsSectionBuilder AddToggle(string id, IModSettingsValueBinding<bool> binding)
		{
			throw new NotSupportedException();
		}

		[ExtensionMarker("<M>$07F5FBE0E78EAC9F936744488E50352C")]
		private ModSettingsSectionBuilder AddSubPage(string pageId)
		{
			throw new NotSupportedException();
		}

		[ExtensionMarker("<M>$07F5FBE0E78EAC9F936744488E50352C")]
		private ModSettingsSectionBuilder AddHeader(string id)
		{
			throw new NotSupportedException();
		}

		[ExtensionMarker("<M>$07F5FBE0E78EAC9F936744488E50352C")]
		private ModSettingsSectionBuilder AddEnumChoice<TValue>(string id, IModSettingsValueBinding<TValue> binding, ModSettingsChoicePresentation presentation = 1) where TValue : struct, Enum
		{
			throw new NotSupportedException();
		}

		[ExtensionMarker("<M>$07F5FBE0E78EAC9F936744488E50352C")]
		private ModSettingsSectionBuilder AddButton(string id, Action action, ModSettingsButtonTone tone = 0)
		{
			throw new NotSupportedException();
		}

		[ExtensionMarker("<M>$07F5FBE0E78EAC9F936744488E50352C")]
		private ModSettingsSectionBuilder AddParagraph(string id)
		{
			throw new NotSupportedException();
		}
	}

	public static void Register()
	{
		RegisterMainPage();
		RegisterOutOfCombatSettingsPage();
		RegisterInCombatSettingsPage();
		RegisterDebugPage();
	}

	private static void RegisterMainPage()
	{
		RitsuLibFramework.RegisterModSettings("RandomForeseer", (Action<ModSettingsPageBuilder>)delegate(ModSettingsPageBuilder page)
		{
			page.WithModDisplayName(T("mod.name")).WithTitle(T("page.title")).WithDescription(T("page.description"))
				.WithSortOrder(0)
				.AddSection("general_prediction", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
				{
					section.WithTitle(T("section.general_prediction.title")).WithDescription(T("section.general_prediction.description")).AddToggle("singleplayer_prediction_enabled", SettingsUiBindings.SingleplayerPredictionEnabled)
						.AddToggle("multiplayer_prediction_enabled", SettingsUiBindings.MultiplayerPredictionEnabled)
						.AddToggle("fair_mode_enabled", SettingsUiBindings.FairModeEnabled)
						.AddToggle("show_drift_warnings", SettingsUiBindings.ShowDriftWarnings);
				})
				.AddSection("prediction_page_navigation", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
				{
					section.WithTitle(T("section.prediction_page_navigation.title")).WithDescription(T("section.prediction_page_navigation.description")).AddSubPage("out_of_combat_prediction")
						.AddSubPage("in_combat_prediction");
				})
				.AddSection("compatibility", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
				{
					section.WithTitle(T("section.compatibility.title")).WithDescription(T("section.compatibility.description")).AddToggle("predict_base_game_cards_only", SettingsUiBindings.PredictBaseGameCardsOnly)
						.AddToggle("invoke_base_game_hook_listeners_only", SettingsUiBindings.InvokeBaseGameHookListenersOnly);
				});
		}, (string)null);
	}

	private static void RegisterOutOfCombatSettingsPage()
	{
		RitsuLibFramework.RegisterModSettings("RandomForeseer", (Action<ModSettingsPageBuilder>)delegate(ModSettingsPageBuilder page)
		{
			page.AsChildOf("RandomForeseer").WithTitle(T("page.out_of_combat_prediction.title")).WithDescription(T("page.out_of_combat_prediction.description"))
				.WithSortOrder(0)
				.AddSection("out_of_combat_prediction_sources", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
				{
					section.WithTitle(T("section.out_of_combat_prediction_sources.title")).WithDescription(T("section.out_of_combat_prediction_sources.description")).AddHeader("general_out_of_combat_predictions")
						.AddToggle("deck_transform_prediction_enabled", SettingsUiBindings.DeckTransformPredictionEnabled)
						.AddToggle("next_act_prediction_enabled", SettingsUiBindings.NextActPredictionEnabled)
						.AddHeader("event_predictions")
						.AddToggle("event_option_prediction_enabled", SettingsUiBindings.EventOptionPredictionEnabled)
						.AddIntSlider("slippery_bridge_reroll_preview_count", T("slider.slippery_bridge_reroll_preview_count.label"), SettingsUiBindings.SlipperyBridgeRerollPreviewCount, 1, 10, 1, (Func<int, string>)null, T("slider.slippery_bridge_reroll_preview_count.description"))
						.WithEntryEnabledWhen("slippery_bridge_reroll_preview_count", (Func<bool>)(() => SettingsUiBindings.EventOptionPredictionEnabled.Read()))
						.AddToggle("crystal_sphere_clairvoyance_enabled", SettingsUiBindings.CrystalSphereClairvoyanceEnabled)
						.AddHeader("relic_predictions")
						.AddToggle("relic_pickup_prediction_enabled", SettingsUiBindings.RelicPickupPredictionEnabled)
						.AddToggle("ancient_relic_pickup_prediction_enabled", SettingsUiBindings.AncientRelicPickupPredictionEnabled)
						.AddToggle("merchant_restock_prediction_enabled", SettingsUiBindings.MerchantRestockPredictionEnabled)
						.AddToggle("driftwood_reroll_prediction_enabled", SettingsUiBindings.DriftwoodRerollPredictionEnabled)
						.AddToggle("paels_wing_sacrifice_prediction_enabled", SettingsUiBindings.PaelsWingSacrificePredictionEnabled)
						.AddToggle("rest_site_prediction_enabled", SettingsUiBindings.RestSitePredictionEnabled);
				});
		}, "out_of_combat_prediction");
	}

	private static void RegisterInCombatSettingsPage()
	{
		RitsuLibFramework.RegisterModSettings("RandomForeseer", (Action<ModSettingsPageBuilder>)delegate(ModSettingsPageBuilder page)
		{
			page.AsChildOf("RandomForeseer").WithTitle(T("page.in_combat_prediction.title")).WithDescription(T("page.in_combat_prediction.description"))
				.WithSortOrder(1)
				.AddSection("in_combat_prediction_sources", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
				{
					section.WithTitle(T("section.in_combat_prediction_sources.title")).WithDescription(T("section.in_combat_prediction_sources.description")).AddHeader("card_and_potion_predictions")
						.AddToggle("card_play_prediction_enabled", SettingsUiBindings.CardPlayPredictionEnabled)
						.AddToggle("potion_prediction_enabled", SettingsUiBindings.PotionPredictionEnabled)
						.AddToggle("combat_transform_prediction_enabled", SettingsUiBindings.CombatTransformPredictionEnabled)
						.AddHeader("end_turn_predictions")
						.AddToggle("end_turn_prediction_enabled", SettingsUiBindings.EndTurnPredictionEnabled)
						.AddEnumChoice<EndTurnPredictionDisplayMode>("end_turn_prediction_display_mode", SettingsUiBindings.EndTurnPredictionDisplayMode, (ModSettingsChoicePresentation)1)
						.AddEnumChoice<EndTurnPredictionDisplayMode>("end_turn_health_bar_forecast_display_mode", SettingsUiBindings.EndTurnHealthBarForecastDisplayMode, (ModSettingsChoicePresentation)1)
						.WithEntryEnabledWhen("end_turn_prediction_display_mode", (Func<bool>)(() => SettingsUiBindings.EndTurnPredictionEnabled.Read()))
						.WithEntryEnabledWhen("end_turn_health_bar_forecast_display_mode", (Func<bool>)(() => SettingsUiBindings.EndTurnPredictionEnabled.Read()))
						.AddHeader("frozen_eye_predictions")
						.AddToggle("frozen_eye_enabled", SettingsUiBindings.FrozenEyeEnabled)
						.AddToggle("shuffle_prediction_enabled", SettingsUiBindings.ShufflePredictionEnabled)
						.WithEntryEnabledWhen("shuffle_prediction_enabled", (Func<bool>)(() => SettingsUiBindings.FrozenEyeEnabled.Read()));
				})
				.AddSection("card_effect_prediction", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
				{
					section.WithTitle(T("section.card_effect_prediction.title")).WithDescription(T("section.card_effect_prediction.description")).WithEnabledWhen((Func<bool>)(() => SettingsUiBindings.CardPlayPredictionEnabled.Read()))
						.AddToggle("combat_card_generation_prediction_enabled", SettingsUiBindings.CombatCardGenerationPredictionEnabled)
						.AddToggle("card_draw_prediction_enabled", SettingsUiBindings.CardDrawPredictionEnabled)
						.AddToggle("combat_card_selection_prediction_enabled", SettingsUiBindings.CombatCardSelectionPredictionEnabled)
						.AddToggle("combat_orb_generation_prediction_enabled", SettingsUiBindings.CombatOrbGenerationPredictionEnabled);
				})
				.AddSection("potion_effect_prediction", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
				{
					section.WithTitle(T("section.potion_effect_prediction.title")).WithDescription(T("section.potion_effect_prediction.description")).WithEnabledWhen((Func<bool>)(() => SettingsUiBindings.PotionPredictionEnabled.Read()))
						.AddToggle("potion_card_generation_prediction_enabled", SettingsUiBindings.PotionCardGenerationPredictionEnabled)
						.AddToggle("potion_draw_prediction_enabled", SettingsUiBindings.PotionDrawPredictionEnabled);
				})
				.AddSection("shared_prediction", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
				{
					section.WithTitle(T("section.shared_prediction.title")).WithDescription(T("section.shared_prediction.description")).WithEnabledWhen((Func<bool>)(() => SettingsUiBindings.CardPlayPredictionEnabled.Read() || SettingsUiBindings.PotionPredictionEnabled.Read()))
						.AddToggle("potion_generation_prediction_enabled", SettingsUiBindings.PotionGenerationPredictionEnabled)
						.AddToggle("auto_play_from_draw_pile_prediction_enabled", SettingsUiBindings.AutoPlayFromDrawPilePredictionEnabled);
				})
				.AddSection("card_resolution_scope", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
				{
					section.WithTitle(T("section.card_resolution_scope.title")).WithDescription(T("section.card_resolution_scope.description")).WithEnabledWhen((Func<bool>)(() => SettingsUiBindings.CardPlayPredictionEnabled.Read() || SettingsUiBindings.PotionPredictionEnabled.Read() || SettingsUiBindings.EndTurnPredictionEnabled.Read()))
						.AddToggle("infer_card_on_play_effects_enabled", SettingsUiBindings.InferCardOnPlayEffectsEnabled)
						.AddToggle("chained_card_effect_prediction_enabled", SettingsUiBindings.ChainedCardEffectPredictionEnabled)
						.WithEntryEnabledWhen("chained_card_effect_prediction_enabled", (Func<bool>)(() => SettingsUiBindings.CardPlayPredictionEnabled.Read() || SettingsUiBindings.PotionPredictionEnabled.Read()));
				})
				.AddSection("damage_prediction", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
				{
					section.WithTitle(T("section.damage_prediction.title")).WithDescription(T("section.damage_prediction.description")).WithEnabledWhen((Func<bool>)(() => SettingsUiBindings.CardPlayPredictionEnabled.Read() || SettingsUiBindings.PotionPredictionEnabled.Read() || SettingsUiBindings.EndTurnPredictionEnabled.Read()))
						.AddToggle("combat_damage_prediction_enabled", SettingsUiBindings.CombatDamagePredictionEnabled)
						.AddToggle("orb_damage_prediction_enabled", SettingsUiBindings.OrbDamagePredictionEnabled)
						.AddToggle("random_target_attack_prediction_enabled", SettingsUiBindings.RandomTargetAttackPredictionEnabled)
						.AddColor("damage_prediction_health_bar_color", T("color.damage_prediction_health_bar_color.label"), SettingsUiBindings.DamagePredictionHealthBarColor, T("color.damage_prediction_health_bar_color.description"), true, false)
						.WithEntryEnabledWhen("orb_damage_prediction_enabled", (Func<bool>)(() => SettingsUiBindings.CombatDamagePredictionEnabled.Read()))
						.WithEntryEnabledWhen("random_target_attack_prediction_enabled", (Func<bool>)(() => SettingsUiBindings.CombatDamagePredictionEnabled.Read()))
						.WithEntryEnabledWhen("damage_prediction_health_bar_color", (Func<bool>)(() => SettingsUiBindings.CombatDamagePredictionEnabled.Read()));
				})
				.AddSection("experimental_features", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
				{
					section.WithTitle(T("section.experimental_features.title")).WithDescription(T("section.experimental_features.description")).Collapsible(true)
						.AddParagraph("no_experimental_features");
				});
		}, "in_combat_prediction");
	}

	private static void RegisterDebugPage()
	{
		RitsuLibFramework.RegisterModSettings("RandomForeseer", (Action<ModSettingsPageBuilder>)delegate(ModSettingsPageBuilder page)
		{
			page.WithTitle(T("page.debug.title")).WithDescription(T("page.debug.description")).WithSortOrder(1)
				.WithVisibleWhen((Func<bool>)(() => SettingsUiBindings.DebugSettingsEnabled.Read()))
				.AddSection("event_debug", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
				{
					section.WithTitle(T("section.event_debug.title")).WithDescription(T("section.event_debug.description")).AddToggle("event_debug_reroll_enabled", SettingsUiBindings.EventDebugRerollEnabled);
				})
				.AddSection("relic_pickup_debug", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
				{
					section.WithTitle(T("section.relic_pickup_debug.title")).WithDescription(T("section.relic_pickup_debug.description")).AddButton("offer_predicted_non_ancient_relics", RelicPickupDebugRewards.OfferPredictedNonAncientRelics, (ModSettingsButtonTone)2)
						.AddButton("open_predicted_treasure_room", RelicPickupDebugRewards.OpenPredictedTreasureRoom, (ModSettingsButtonTone)2)
						.AddButton("open_relic_trader_pickup_test", RelicPickupDebugRewards.OpenRelicTraderPickupTest, (ModSettingsButtonTone)2);
				})
				.AddSection("telemetry_debug", (Action<ModSettingsSectionBuilder>)delegate(ModSettingsSectionBuilder section)
				{
					section.WithTitle(T("section.telemetry_debug.title")).WithDescription(T("section.telemetry_debug.description")).AddButton("capture_manual_telemetry_exception", TelemetryDebugActions.CaptureManualException, (ModSettingsButtonTone)2)
						.AddButton("capture_automatic_telemetry_exception", TelemetryDebugActions.CaptureAutomaticException, (ModSettingsButtonTone)2);
				});
		}, "debug");
	}

	private static ModSettingsText T(string key)
	{
		return ModSettingsText.I18N(ModLocalization.SettingsLocalization, key, key);
	}

	private static ModSettingsSectionBuilder AddToggle(this ModSettingsSectionBuilder section, string id, IModSettingsValueBinding<bool> binding)
	{
		return section.AddToggle(id, T("toggle." + id + ".label"), binding, T("toggle." + id + ".description"), (Func<bool>)null);
	}

	private static ModSettingsSectionBuilder AddSubPage(this ModSettingsSectionBuilder section, string pageId)
	{
		return section.AddSubpage("open_" + pageId + "_page", T("page." + pageId + ".title"), pageId, T("button.open_page.text"), T("page." + pageId + ".description"));
	}

	private static ModSettingsSectionBuilder AddHeader(this ModSettingsSectionBuilder section, string id)
	{
		return section.AddHeader(id, T("header." + id + ".label"), (ModSettingsText)null);
	}

	private static ModSettingsSectionBuilder AddEnumChoice<TValue>(this ModSettingsSectionBuilder section, string id, IModSettingsValueBinding<TValue> binding, ModSettingsChoicePresentation presentation = 1) where TValue : struct, Enum
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		string id2 = id;
		return section.AddEnumChoice<TValue>(id2, T("choice." + id2 + ".label"), binding, (Func<TValue, ModSettingsText>)((TValue value) => T($"choice.{id2}.option.{value}")), T("choice." + id2 + ".description"), presentation);
	}

	private static ModSettingsSectionBuilder AddButton(this ModSettingsSectionBuilder section, string id, Action action, ModSettingsButtonTone tone = 0)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		return section.AddButton(id, T("button." + id + ".label"), T("button." + id + ".text"), action, tone, T("button." + id + ".description"));
	}

	private static ModSettingsSectionBuilder AddParagraph(this ModSettingsSectionBuilder section, string id)
	{
		return section.AddParagraph(id, T("paragraph." + id + ".text"), (ModSettingsText)null, (float?)null);
	}
}
