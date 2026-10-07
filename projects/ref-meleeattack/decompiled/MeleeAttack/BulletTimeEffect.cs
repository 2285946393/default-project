using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class BulletTimeEffect
{
	[HarmonyPatch(typeof(CardModel), "OnEnqueuePlayVfx")]
	private static class BulletTimeCardPlayPatch
	{
		private static void Prefix(CardModel __instance)
		{
			if (!SettingsUI.IsBulletTimeEnabled())
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
			if ((string?)obj != "BULLET_TIME")
			{
				return;
			}
			Player owner = __instance.Owner;
			Creature val = ((owner != null) ? owner.Creature : null);
			if (val != null)
			{
				PowerModel val2 = FindNoDrawPowerOn(val);
				if (val2 != null)
				{
					Log.Info("[BulletTimeEffect] 检测到 BULLET_TIME 打出，玩家已有不可抽牌 buff，直接启动", 2);
					StartBulletTime(val, val2);
					_lastBulletTimePlayer = null;
				}
				else
				{
					_lastBulletTimePlayer = val;
					_lastBulletTimeTime = (float)((double)Time.GetTicksMsec() / 1000.0);
					Log.Info("[BulletTimeEffect] 检测到 BULLET_TIME 打出，等待不可抽牌 buff 施加...", 2);
				}
			}
		}
	}

	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	private static class CombatReadySubscribePatch
	{
		private static void Postfix(NCombatRoom __instance)
		{
			if (__instance == null)
			{
				return;
			}
			foreach (NCreature creatureNode in __instance.CreatureNodes)
			{
				if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode) && creatureNode.Entity != null)
				{
					creatureNode.Entity.PowerApplied -= OnCreaturePowerApplied;
					creatureNode.Entity.PowerApplied += OnCreaturePowerApplied;
				}
			}
			Cleanup();
		}
	}

	[HarmonyPatch(typeof(NCombatRoom), "_ExitTree")]
	private static class CombatExitPatch
	{
		private static void Postfix(NCombatRoom __instance)
		{
			if (__instance != null)
			{
				foreach (NCreature creatureNode in __instance.CreatureNodes)
				{
					if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode) && creatureNode.Entity != null)
					{
						creatureNode.Entity.PowerApplied -= OnCreaturePowerApplied;
					}
				}
			}
			Cleanup();
		}
	}

	private static bool _isActive;

	private static Creature _player;

	private static Creature _lastBulletTimePlayer;

	private static float _lastBulletTimeTime;

	private const float BULLET_TIME_WINDOW = 1.5f;

	private static PowerModel _activeNoDrawPower;

	private static CanvasLayer _grayLayer;

	private static ColorRect _grayRect;

	private static bool IsNoDrawPower(PowerModel power)
	{
		if (power == null)
		{
			return false;
		}
		string name = ((object)power).GetType().Name;
		return name.Contains("NoDraw") || name.Contains("CannotDraw") || name.Contains("Drawless") || name.Contains("DrawLock") || name.Contains("NoCardDraw") || name.Contains("BulletTime");
	}

	private static Creature GetPowerOwner(PowerModel power)
	{
		if (power == null)
		{
			return null;
		}
		Type type = ((object)power).GetType();
		PropertyInfo propertyInfo = type.GetProperty("Owner") ?? type.GetProperty("owner") ?? type.GetProperty("Creature") ?? type.GetProperty("creature");
		if (propertyInfo != null)
		{
			try
			{
				object? value = propertyInfo.GetValue(power);
				return (Creature)((value is Creature) ? value : null);
			}
			catch
			{
			}
		}
		return null;
	}

	private static PowerModel FindNoDrawPowerOn(Creature creature)
	{
		if (creature == null)
		{
			return null;
		}
		try
		{
			IReadOnlyList<PowerModel> powers = creature.Powers;
			if (powers == null)
			{
				return null;
			}
			foreach (PowerModel item in powers)
			{
				if (item != null && IsNoDrawPower(item))
				{
					return item;
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("[BulletTimeEffect] FindNoDrawPowerOn 异常: " + ex.Message, 2);
		}
		return null;
	}

	private static void ApplyGrayFilter()
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Expected O, but got Unknown
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Expected O, but got Unknown
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Expected O, but got Unknown
		if (_grayLayer == null)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			object obj;
			if (instance == null)
			{
				obj = null;
			}
			else
			{
				SceneTree tree = ((Node)instance).GetTree();
				obj = ((tree != null) ? tree.Root : null);
			}
			Window val = (Window)obj;
			if (val != null)
			{
				_grayLayer = new CanvasLayer
				{
					Layer = 100,
					Name = StringName.op_Implicit("BulletTimeGrayFilter")
				};
				_grayRect = new ColorRect
				{
					AnchorLeft = 0f,
					AnchorTop = 0f,
					AnchorRight = 1f,
					AnchorBottom = 1f,
					Size = Vector2.Zero,
					MouseFilter = (MouseFilterEnum)2,
					Color = Colors.White,
					Modulate = new Color(1f, 1f, 1f, 1f),
					Visible = true
				};
				Shader val2 = new Shader();
				val2.Code = "\r\n                shader_type canvas_item;\r\n                render_mode unshaded;\r\n                uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;\r\n                void fragment() {\r\n                    vec4 c = texture(SCREEN_TEXTURE, SCREEN_UV);\r\n                    float luma = dot(c.rgb, vec3(0.299, 0.587, 0.114));\r\n                    COLOR = vec4(vec3(luma), 1.0);\r\n                }\r\n            ";
				((CanvasItem)_grayRect).Material = (Material)new ShaderMaterial
				{
					Shader = val2
				};
				((Node)_grayLayer).AddChild((Node)(object)_grayRect, false, (InternalMode)0);
				((Node)val).AddChild((Node)(object)_grayLayer, false, (InternalMode)0);
				Log.Info("[BulletTimeEffect] 全屏灰度去色滤镜已激活", 2);
			}
		}
	}

	private static void RemoveGrayFilter()
	{
		if (_grayLayer != null)
		{
			((Node)_grayLayer).QueueFree();
			_grayLayer = null;
			_grayRect = null;
			Log.Info("[BulletTimeEffect] 全屏灰度去色滤镜已移除", 2);
		}
	}

	private static void StartBulletTime(Creature creature, PowerModel noDrawPower)
	{
		if (!_isActive && creature != null)
		{
			_isActive = true;
			_player = creature;
			_activeNoDrawPower = noDrawPower;
			AttackTimeTerminator.TriggerAttackTime(creature);
			ApplyGrayFilter();
			FilterTrigger.StartFilter(creature);
			AttackTimeState.OnEnd += OnBulletTimeEnd;
			Log.Info("[BulletTimeEffect] 子弹时间已启动（由不可抽牌 buff 触发）", 2);
		}
	}

	private static void OnBulletTimeEnd()
	{
		EndBulletTime();
	}

	private static void EndBulletTime()
	{
		if (_isActive)
		{
			_isActive = false;
			RemoveGrayFilter();
			DistortionFilter.FadeOut();
			if (_player != null)
			{
				FilterTrigger.StartFilter(_player);
			}
			AttackTimeState.OnEnd -= OnBulletTimeEnd;
			_activeNoDrawPower = null;
			_player = null;
			Log.Info("[BulletTimeEffect] 子弹时间已结束（不可抽牌 buff 消失）", 2);
		}
	}

	internal static void Cleanup()
	{
		if (_isActive)
		{
			EndBulletTime();
		}
		if (_grayLayer != null)
		{
			((Node)_grayLayer).QueueFree();
			_grayLayer = null;
			_grayRect = null;
		}
		_isActive = false;
		_player = null;
		_activeNoDrawPower = null;
		_lastBulletTimePlayer = null;
		AttackTimeState.OnEnd -= OnBulletTimeEnd;
	}

	internal static void OnCreaturePowerApplied(PowerModel power)
	{
		if (power == null || _isActive || !IsNoDrawPower(power))
		{
			return;
		}
		Creature powerOwner = GetPowerOwner(power);
		if (powerOwner != null)
		{
			float num = (float)((double)Time.GetTicksMsec() / 1000.0);
			if (_lastBulletTimePlayer == powerOwner && !(num - _lastBulletTimeTime > 1.5f))
			{
				StartBulletTime(powerOwner, power);
				_lastBulletTimePlayer = null;
			}
		}
	}

	private static void OnPowerRemoved(PowerModel power)
	{
		if (_isActive && power != null && power == _activeNoDrawPower)
		{
			EndBulletTime();
		}
	}

	private static void OnPowerStackChanged(PowerModel power, int oldStack, int newStack)
	{
		if (_isActive && power != null && power == _activeNoDrawPower && newStack == 0)
		{
			EndBulletTime();
		}
	}

	static BulletTimeEffect()
	{
		_isActive = false;
		_player = null;
		_lastBulletTimePlayer = null;
		_lastBulletTimeTime = 0f;
		_activeNoDrawPower = null;
		_grayLayer = null;
		_grayRect = null;
		BuffMonitor.PowerRemoved += OnPowerRemoved;
		BuffMonitor.PowerStackChanged += OnPowerStackChanged;
	}
}
