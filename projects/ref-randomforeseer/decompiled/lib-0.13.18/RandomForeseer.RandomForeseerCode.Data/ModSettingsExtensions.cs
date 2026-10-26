using System;
using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Settings;

namespace RandomForeseer.RandomForeseerCode.Data;

internal static class ModSettingsExtensions
{
	[SpecialName]
	public sealed class _003CG_003E_00240BBB80E5049EAABE9B0B48F1EEF0F6AD
	{
		[SpecialName]
		public static class _003CM_003E_0024FCD9EDBF0BB15554F82CC0580A6CE1B5
		{
		}

		[ExtensionMarker("<M>$FCD9EDBF0BB15554F82CC0580A6CE1B5")]
		public bool IsPredictionEnabled
		{
			[ExtensionMarker("<M>$FCD9EDBF0BB15554F82CC0580A6CE1B5")]
			get
			{
				throw new NotSupportedException();
			}
		}

		[ExtensionMarker("<M>$FCD9EDBF0BB15554F82CC0580A6CE1B5")]
		public Color DamagePredictionHealthBarColorValue
		{
			[ExtensionMarker("<M>$FCD9EDBF0BB15554F82CC0580A6CE1B5")]
			get
			{
				throw new NotSupportedException();
			}
		}

		[ExtensionMarker("<M>$FCD9EDBF0BB15554F82CC0580A6CE1B5")]
		public bool Allows(PredictionFairness fairness)
		{
			throw new NotSupportedException();
		}
	}

	public static bool get_IsPredictionEnabled(ModSettings settings)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Invalid comparison between Unknown and I4
		NetGameType currentNetGameType = GetCurrentNetGameType();
		if ((int)currentNetGameType != 1)
		{
			if (currentNetGameType - 2 <= 1)
			{
				return settings.MultiplayerPredictionEnabled;
			}
			return false;
		}
		return settings.SingleplayerPredictionEnabled;
	}

	public static Color get_DamagePredictionHealthBarColorValue(ModSettings settings)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		Color result = default(Color);
		if (!ModSettingsColorControl.TryDeserializeColorForSettings(settings.DamagePredictionHealthBarColor, ref result))
		{
			return new Color(ModSettings.Default.DamagePredictionHealthBarColor);
		}
		return result;
	}

	public static bool Allows(this ModSettings settings, PredictionFairness fairness)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Invalid comparison between Unknown and I4
		if (!settings.FairModeEnabled)
		{
			return true;
		}
		return fairness switch
		{
			PredictionFairness.Fair => true, 
			PredictionFairness.UnfairInSingleplayer => (int)GetCurrentNetGameType() != 1, 
			PredictionFairness.UnfairInAllModes => false, 
			_ => throw new ArgumentOutOfRangeException("fairness", fairness, "Unknown fairness value"), 
		};
	}

	private static NetGameType GetCurrentNetGameType()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		RunManager instance = RunManager.Instance;
		if (instance != null && instance.IsInProgress)
		{
			INetGameService netService = instance.NetService;
			if (netService != null)
			{
				return netService.Type;
			}
		}
		return (NetGameType)0;
	}
}
