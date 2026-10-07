using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

[HarmonyPatch(typeof(CardModel), "OnEnqueuePlayVfx")]
public static class WraithFormFilterPatch
{
	private static readonly Dictionary<CardModel, float> _recentlyTriggered = new Dictionary<CardModel, float>();

	private static readonly object _dedupLock = new object();

	private static bool ShouldTrigger(CardModel card)
	{
		if (card == null)
		{
			return false;
		}
		float num = (float)((double)Time.GetTicksMsec() / 1000.0);
		lock (_dedupLock)
		{
			if (_recentlyTriggered.TryGetValue(card, out var value) && num - value < 0.2f)
			{
				return false;
			}
			_recentlyTriggered[card] = num;
			if (_recentlyTriggered.Count > 32)
			{
				List<CardModel> list = new List<CardModel>();
				foreach (KeyValuePair<CardModel, float> item in _recentlyTriggered)
				{
					if (num - item.Value > 1f)
					{
						list.Add(item.Key);
					}
				}
				foreach (CardModel item2 in list)
				{
					_recentlyTriggered.Remove(item2);
				}
			}
			return true;
		}
	}

	public static void Prefix(CardModel __instance)
	{
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		if (!SettingsUI.IsWraithFormEnabled())
		{
			return;
		}
		object obj;
		if (__instance == null)
		{
			obj = null;
		}
		else
		{
			ModelId id = ((AbstractModel)__instance).Id;
			obj = ((id != null) ? id.Entry : null);
		}
		if (obj == null || !((AbstractModel)__instance).Id.Entry.Equals("WRAITH_FORM", StringComparison.OrdinalIgnoreCase) || !ShouldTrigger(__instance))
		{
			return;
		}
		Player owner = __instance.Owner;
		Creature val = ((owner != null) ? owner.Creature : null);
		if (val != null)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature val2 = ((instance != null) ? instance.GetCreatureNode(val) : null);
			if (val2 != null && GodotObject.IsInstanceValid((GodotObject)(object)val2))
			{
				Color targetColor = RoleColorFilter.GetTargetColor(val);
				RoleColorFilter.ApplyFilter(val2, targetColor);
			}
		}
	}
}
