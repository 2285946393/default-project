using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace MeleeAttack;

public static class BuffMonitor
{
	[HarmonyPatch(typeof(PowerCmd), "Remove", new Type[] { typeof(PowerModel) })]
	private static class PowerRemovePatch
	{
		private static void Postfix(PowerModel power)
		{
			if (power == null)
			{
				return;
			}
			try
			{
				BuffMonitor.PowerRemoved?.Invoke(power);
				BuffMonitor.AnyPowerEvent?.Invoke(power);
			}
			catch (Exception ex)
			{
				Log.Warn("[BuffMonitor] PowerRemoved error: " + ex.Message, 2);
			}
		}
	}

	[HarmonyPatch(typeof(PowerCmd), "Decrement", new Type[] { typeof(PowerModel) })]
	private static class PowerDecrementPatch
	{
		private static void Postfix(PowerModel power)
		{
			if (power == null)
			{
				return;
			}
			try
			{
				int stackCount = GetStackCount(power);
				BuffMonitor.PowerStackChanged?.Invoke(power, -1, stackCount);
				BuffMonitor.AnyPowerEvent?.Invoke(power);
			}
			catch (Exception ex)
			{
				Log.Warn("[BuffMonitor] PowerStackChanged error: " + ex.Message, 2);
			}
		}

		private static int GetStackCount(PowerModel power)
		{
			PropertyInfo property = ((object)power).GetType().GetProperty("StackCount");
			if (property != null && property.CanRead)
			{
				try
				{
					return (int)property.GetValue(power);
				}
				catch
				{
				}
			}
			string[] array = new string[5] { "Stacks", "Amount", "Count", "stackCount", "stacks" };
			string[] array2 = array;
			foreach (string name in array2)
			{
				property = ((object)power).GetType().GetProperty(name);
				if (property != null && property.CanRead)
				{
					try
					{
						return (int)property.GetValue(power);
					}
					catch
					{
					}
				}
			}
			string[] array3 = new string[6] { "_stackCount", "_stacks", "_amount", "_count", "stackCount", "stacks" };
			string[] array4 = array3;
			foreach (string name2 in array4)
			{
				FieldInfo field = ((object)power).GetType().GetField(name2, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (field != null && field.FieldType == typeof(int))
				{
					try
					{
						return (int)field.GetValue(power);
					}
					catch
					{
					}
				}
			}
			Log.Warn("[BuffMonitor] 无法读取 " + ((object)power).GetType().Name + " 的层数，请检查属性名。", 2);
			return 0;
		}
	}

	public static event Action<PowerModel> PowerRemoved;

	public static event Action<PowerModel, int, int> PowerStackChanged;

	public static event Action<PowerModel> AnyPowerEvent;
}
