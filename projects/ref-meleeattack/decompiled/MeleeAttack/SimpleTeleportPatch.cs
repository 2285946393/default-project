using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeleeAttack;

public static class SimpleTeleportPatch
{
	[HarmonyPatch(typeof(CreatureCmd), "TriggerAnim", new Type[]
	{
		typeof(Creature),
		typeof(string),
		typeof(float)
	})]
	private static class DefectAttackResetPatch
	{
		private static void Prefix(Creature creature, string triggerName)
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Invalid comparison between Unknown and I4
			if (creature != null && !string.IsNullOrEmpty(triggerName) && (int)creature.Side == 1 && triggerName.Contains("Attack", StringComparison.OrdinalIgnoreCase))
			{
				_defectEffectTriggered = false;
			}
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class DefectDamageReceivedPatch
	{
		private static void Postfix(IRunState runState, ICombatState combatState, Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource)
		{
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Invalid comparison between Unknown and I4
			if (_defectEffectTriggered || dealer == null || (int)dealer.Side != 1 || target == null || target.IsPlayer || cardSource == null)
			{
				return;
			}
			CardPoolModel pool = cardSource.Pool;
			if (pool == null || !string.Equals(pool.Title, "Defect", StringComparison.OrdinalIgnoreCase) || !cardSource.IsUpgraded)
			{
				return;
			}
			_defectEffectTriggered = true;
			AttackCommand value = _currentAttack.Value;
			if (value != null)
			{
				List<Creature> aliveTargets = GetAliveTargets(value);
				if (aliveTargets.Count != 0)
				{
					PlayDefectUpgradedEffect(dealer, aliveTargets);
				}
			}
		}
	}

	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	private static class DefectResetPatch
	{
		private static void Postfix()
		{
			_defectEffectTriggered = false;
		}
	}

	[HarmonyPatch(typeof(CreatureCmd), "TriggerAnim", new Type[]
	{
		typeof(Creature),
		typeof(string),
		typeof(float)
	})]
	private static class IroncladAttackResetPatch
	{
		private static void Prefix(Creature creature, string triggerName)
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Invalid comparison between Unknown and I4
			if (creature != null && !string.IsNullOrEmpty(triggerName) && (int)creature.Side == 1 && triggerName.Contains("Attack", StringComparison.OrdinalIgnoreCase))
			{
				_ironcladEffectTriggered = false;
			}
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class IroncladDamageReceivedPatch
	{
		private static void Postfix(IRunState runState, ICombatState combatState, Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource)
		{
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Invalid comparison between Unknown and I4
			if (_ironcladEffectTriggered || dealer == null || (int)dealer.Side != 1 || target == null || target.IsPlayer || cardSource == null)
			{
				return;
			}
			CardPoolModel pool = cardSource.Pool;
			if (pool == null || !string.Equals(pool.Title, "Ironclad", StringComparison.OrdinalIgnoreCase) || !cardSource.IsUpgraded)
			{
				return;
			}
			_ironcladEffectTriggered = true;
			AttackCommand value = _currentAttack.Value;
			if (value != null)
			{
				List<Creature> aliveTargets = GetAliveTargets(value);
				if (aliveTargets.Count != 0)
				{
					PlayIroncladUpgradedEffect(dealer, aliveTargets);
				}
			}
		}
	}

	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	private static class IroncladResetPatch
	{
		private static void Postfix()
		{
			_ironcladEffectTriggered = false;
		}
	}

	[HarmonyPatch(typeof(CreatureCmd), "TriggerAnim", new Type[]
	{
		typeof(Creature),
		typeof(string),
		typeof(float)
	})]
	private static class NecroAttackResetPatch
	{
		private static void Prefix(Creature creature, string triggerName)
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Invalid comparison between Unknown and I4
			if (creature != null && !string.IsNullOrEmpty(triggerName) && (int)creature.Side == 1 && triggerName.Contains("Attack", StringComparison.OrdinalIgnoreCase))
			{
				_necrobinderEffectTriggered = false;
			}
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class NecroDamageReceivedPatch
	{
		private static void Postfix(IRunState runState, ICombatState combatState, Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource)
		{
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Invalid comparison between Unknown and I4
			if (!_necrobinderEffectTriggered && dealer != null && (int)dealer.Side == 1 && target != null && !target.IsPlayer && cardSource != null)
			{
				CardPoolModel pool = cardSource.Pool;
				if (pool != null && string.Equals(pool.Title, "Necrobinder", StringComparison.OrdinalIgnoreCase) && cardSource.IsUpgraded)
				{
					_necrobinderEffectTriggered = true;
					PlayNecroAttackEffect();
				}
			}
		}
	}

	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	private static class NecroResetPatch
	{
		private static void Postfix()
		{
			_necrobinderEffectTriggered = false;
		}
	}

	[HarmonyPatch(typeof(AttackCommand), "Execute")]
	private static class AttackCommandExecutePatch
	{
		public static void Prefix(AttackCommand __instance)
		{
			_currentAttack.Value = __instance;
			_currentHitIndex.Value = 0;
		}

		public static void Postfix()
		{
			_currentAttack.Value = null;
			_currentHitIndex.Value = 0;
		}
	}

	[HarmonyPatch(typeof(CreatureCmd), "TriggerAnim", new Type[]
	{
		typeof(Creature),
		typeof(string),
		typeof(float)
	})]
	private static class GrandFinaleAttackResetPatch
	{
		private static void Prefix(Creature creature, string triggerName)
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Invalid comparison between Unknown and I4
			if (creature != null && !string.IsNullOrEmpty(triggerName) && (int)creature.Side == 1 && triggerName.Contains("Attack", StringComparison.OrdinalIgnoreCase))
			{
				_grandFinaleEffectTriggered = false;
			}
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class GrandFinaleDamageReceivedPatch
	{
		private static void Postfix(IRunState runState, ICombatState combatState, Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource)
		{
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Invalid comparison between Unknown and I4
			if (_grandFinaleEffectTriggered || dealer == null || (int)dealer.Side != 1 || target == null || target.IsPlayer || cardSource == null)
			{
				return;
			}
			bool flag = false;
			bool flag2 = false;
			CardPoolModel pool = cardSource.Pool;
			if (pool != null && string.Equals(pool.Title, "Silent", StringComparison.OrdinalIgnoreCase))
			{
				flag = cardSource.IsUpgraded;
			}
			flag2 = (cardSource.Tags.Contains((CardTag)5) || ((AbstractModel)cardSource).Id.Entry.IndexOf("Shiv", StringComparison.OrdinalIgnoreCase) >= 0) && cardSource.IsUpgraded;
			if (!flag && !flag2)
			{
				return;
			}
			_grandFinaleEffectTriggered = true;
			AttackCommand value = _currentAttack.Value;
			if (value != null)
			{
				List<Creature> aliveTargets = GetAliveTargets(value);
				if (aliveTargets.Count != 0)
				{
					PlayGrandFinaleEffect(dealer, aliveTargets);
				}
			}
		}
	}

	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	private static class GrandFinaleResetPatch
	{
		private static void Postfix()
		{
			_grandFinaleEffectTriggered = false;
		}
	}

	[HarmonyPatch(typeof(CreatureCmd), "TriggerAnim", new Type[]
	{
		typeof(Creature),
		typeof(string),
		typeof(float)
	})]
	private static class CreatureCmdTriggerAnimPatch
	{
		public static bool Prefix(Creature creature, string triggerName, float waitTime, ref Task __result)
		{
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			//IL_0306: Unknown result type (might be due to invalid IL or missing references)
			//IL_030c: Invalid comparison between Unknown and I4
			string value = "null";
			try
			{
				object obj;
				if (creature == null)
				{
					obj = null;
				}
				else
				{
					MonsterModel monster = creature.Monster;
					if (monster == null)
					{
						obj = null;
					}
					else
					{
						ModelId id = ((AbstractModel)monster).Id;
						obj = ((id != null) ? id.Entry : null);
					}
				}
				if (obj == null)
				{
					obj = "null";
				}
				value = (string)obj;
			}
			catch
			{
			}
			GD.Print($"[MeleeDebug][AnimPatch] ENTER name={((creature != null) ? creature.Name : null)} side={((creature != null) ? new CombatSide?(creature.Side) : null)} monsterId={value} trigger='{triggerName}' wait={waitTime} proxy={_isProxyRunning.Value}");
			if (_isProxyRunning.Value)
			{
				GD.Print("[MeleeDebug][AnimPatch]   -> 放行(_isProxyRunning=true)");
				return true;
			}
			if (creature == null || string.IsNullOrEmpty(triggerName))
			{
				GD.Print("[MeleeDebug][AnimPatch]   -> 放行(creature/triggerName 为空)");
				return true;
			}
			try
			{
				if (NCombatRoom.Instance == null)
				{
					GD.Print("[MeleeDebug][AnimPatch]   -> 放行(NCombatRoom.Instance==null)");
					return true;
				}
			}
			catch
			{
				GD.Print("[MeleeDebug][AnimPatch]   -> 放行(NCombatRoom 访问异常)");
				return true;
			}
			bool flag = IsAttackTriggerName(triggerName);
			GD.Print($"[MeleeDebug][AnimPatch]   isAttack={flag}");
			if (!flag)
			{
				GD.Print("[MeleeDebug][AnimPatch]   -> 放行(非攻击触发名，未命中白名单前缀)");
				return true;
			}
			AttackCommand value2 = _currentAttack.Value;
			GD.Print("[MeleeDebug][AnimPatch]   _currentAttack=" + ((value2 == null) ? "null" : ((object)value2).GetType().Name));
			if (value2 == null)
			{
				GD.Print("[MeleeDebug][AnimPatch]   -> 放行(_currentAttack 为 null，可能是非攻击牌命令)");
				return true;
			}
			if (TryGetCreatureNode(creature, out var _, out var node) && node != null)
			{
				Toutou.OnAttackStart(node);
			}
			else
			{
				GD.Print("[MeleeDebug][AnimPatch]   WARN: TryGetCreatureNode 失败，未调用 Toutou.OnAttackStart");
			}
			List<Creature> aliveTargets = GetAliveTargets(value2);
			GD.Print($"[MeleeDebug][AnimPatch]   targets.Count={aliveTargets.Count}");
			if (aliveTargets.Count == 0)
			{
				GD.Print("[MeleeDebug][AnimPatch]   -> 放行(没有存活目标)");
				return true;
			}
			int value3 = _currentHitIndex.Value;
			_currentHitIndex.Value = value3 + 1;
			GD.Print($"[MeleeDebug][AnimPatch]   hitIndex={value3}");
			if ((int)creature.Side == 1)
			{
				string creatureId = GetCreatureId(creature);
				bool flag2 = SettingsUI.IsTeleportEnabledForCharacter(creatureId);
				GD.Print($"[MeleeDebug][AnimPatch]   PLAYER charId='{creatureId}' teleportEnabled={flag2}");
				if (!flag2)
				{
					GD.Print("[MeleeDebug][AnimPatch]   -> 玩家角色未启用闪现，走原动画");
					__result = InvokeOriginalTriggerAnim(creature, triggerName, waitTime);
					return false;
				}
				GD.Print("[MeleeDebug][AnimPatch]   >>> 玩家闪现攻击 RunTeleportAttack");
				__result = RunTeleportAttack(creature, triggerName, waitTime, aliveTargets, value3);
				return false;
			}
			GD.Print($"[MeleeDebug][AnimPatch]   MONSTER 分支 hitIndex={value3}");
			if (value3 == 0)
			{
				GD.Print("[MeleeDebug][AnimPatch]   >>> 怪物闪现攻击 RunMonsterTeleportAttack");
				__result = MonsterTeleport.RunMonsterTeleportAttack(creature, triggerName, waitTime, aliveTargets, value3);
				return false;
			}
			GD.Print($"[MeleeDebug][AnimPatch]   -> 怪物后续段(hitIndex={value3})，走原动画");
			__result = InvokeOriginalTriggerAnim(creature, triggerName, waitTime);
			return false;
		}
	}

	private struct MovementResult
	{
		public Vector2 TeleportPos;

		public bool LockEnabled;

		public Task MoveTask;
	}

	internal class Vector2Wrapper
	{
		public Vector2 Value;

		public Vector2Wrapper(Vector2 value)
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			Value = value;
		}
	}

	internal class ColorWrapper
	{
		public Color Value;

		public ColorWrapper(Color value)
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			Value = value;
		}
	}

	internal class IntWrapper
	{
		public int Value;

		public IntWrapper(int value)
		{
			Value = value;
		}
	}

	internal class FloatWrapper
	{
		public float Value;

		public FloatWrapper(float value)
		{
			Value = value;
		}
	}

	internal class AttackSequenceState
	{
		public Vector2 LockedHomePos { get; set; }

		public Vector2 LockedHomeLocal { get; set; }

		public bool IsReturning { get; set; }

		public CancellationTokenSource ReturnCts { get; set; }

		public TaskCompletionSource<bool> ReturnCompletion { get; set; }

		public bool IsReturnDelayActive { get; set; }

		public CancellationTokenSource ReturnDelayCts { get; set; }

		public float BaseDelay { get; set; }

		public float ExtraDelayAccumulated { get; set; }

		public float ElapsedDelay { get; set; }

		public float MaxTotalDelay { get; set; } = 3f;


		public float PendingExtraDelay { get; set; }

		public Vector2? VisualOriginalPosition { get; set; }

		public float? VisualOriginalRotation { get; set; }

		public bool IsBackflipState { get; set; }

		public Vector2 BodySlamFacingDir { get; set; } = Vector2.Zero;


		public bool SkipFadeAndStretchOnReturn { get; set; } = false;


		public Vector2 LastAttackTargetCenter { get; set; } = Vector2.Zero;


		public bool HasLastAttackTarget { get; set; } = false;

	}

	[HarmonyPatch(typeof(NCreature), "OstyScaleToSize", new Type[]
	{
		typeof(float),
		typeof(double)
	})]
	private static class NCreatureOstyScaleToSizePatch
	{
		[HarmonyPrefix]
		private static bool Prefix(NCreature __instance, float ostyHealth, double duration)
		{
			if (__instance == null)
			{
				return true;
			}
			if (ostyHealth <= 0f)
			{
				return true;
			}
			Creature entity = __instance.Entity;
			if (entity == null)
			{
				return true;
			}
			if (entity.PetOwner == null)
			{
				return true;
			}
			bool flag;
			try
			{
				flag = LocalContext.IsMe(entity.PetOwner);
			}
			catch
			{
				return true;
			}
			if (!flag)
			{
				return true;
			}
			bool flag2 = IsPlayerMainAttackInProgress();
			bool flag3 = HasActiveAttackState(__instance);
			if (flag2 || flag3)
			{
				ApplyOstyScaleOnly(__instance, ostyHealth);
				return false;
			}
			return true;
		}
	}

	private static bool _defectEffectTriggered = false;

	private static bool _ironcladEffectTriggered = false;

	public const string WHIRLWIND_CARD_ID = "WHIRLWIND";

	private const float WHIRLWIND_FLIP_INTERVAL = 0.1f;

	private const string WHIRLWIND_VFX_PATH = "res://scenes/旋风斩.tscn";

	private const float WHIRLWIND_VFX_FADE_IN = 0.2f;

	private const float WHIRLWIND_VFX_FADE_OUT = 0.2f;

	private const int WHIRLWIND_VFX_RELATIVE_Z = 3;

	internal static readonly ConcurrentDictionary<NCreature, Node2D> _whirlwindVfx = new ConcurrentDictionary<NCreature, Node2D>();

	private static bool _necrobinderEffectTriggered = false;

	private const string NECRO_VFX_PATH = "res://scenes/vfx/ui/vfx_ui_epoch_unlock_chain_shards.tscn";

	private const int NECRO_Z_INDEX = 0;

	private static readonly PackedScene _necroBuffScene = GD.Load<PackedScene>("res://scenes/vfx/ui/vfx_ui_epoch_unlock_chain_shards.tscn");

	private const float DEFECT_DASH_DURATION = 0.25f;

	private const float DEFECT_STRETCH_PEAK = 3f;

	private const float DEFECT_STRETCH_EXP = 6f;

	private const float DEFECT_STRETCH_PEAK_PROGRESS = 0.75f;

	private const float IRONCLAD_DASH_DURATION = 0.2f;

	private const float IRONCLAD_SLASH_STRETCH_PEAK = 2f;

	private const float IRONCLAD_SLASH_STRETCH_EXP = 6f;

	private const float IRONCLAD_SLASH_STRETCH_PEAK_PROGRESS = 0.35f;

	private const float IRONCLAD_HEAVY_STRETCH_PEAK = 1.6f;

	private const float IRONCLAD_HEAVY_STRETCH_EXP = 4.5f;

	private const float IRONCLAD_HEAVY_STRETCH_PEAK_PROGRESS = 0.8f;

	private const float JUMP_HEIGHT_RATIO = 0.3f;

	private const float JUMP_PEAK_PROGRESS = 0.5f;

	public const float GENERIC_DASH_DURATION = 0.2f;

	public const float GENERIC_STRETCH_PEAK = 2f;

	public const float GENERIC_STRETCH_EXP = 6f;

	public const float GENERIC_STRETCH_PEAK_PROGRESS = 0.7f;

	private static bool _grandFinaleEffectTriggered = false;

	public static readonly HashSet<string> _attackTriggerPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Attack", "heavyAttack", "MultiAttack", "SlashTrigger", "Chomp", "SwipePower" };

	public static readonly HashSet<string> _silentDisplacementExcludedCards = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "DAGGER_SPRAY", "DAGGER_THROW", "FLECHETTES" };

	public static readonly Dictionary<string, float> _extraBufferByCharacter = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
	{
		["Ironclad"] = 0.4f,
		["Silent"] = 0.3f,
		["Defect"] = 0.4f,
		["Necrobinder"] = 0.4f,
		["Byrd"] = 1f,
		["Osty"] = 0.3f,
		["Regent"] = 0.4f
	};

	public const float DEFAULT_IRONCLAD_HEAVY_EXTRA_DELAY = 0.35f;

	public const float DEFAULT_EXTRA_BUFFER = 0.4f;

	public static readonly Dictionary<string, float> _multiHitExtraDelayByCharacter = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
	{
		["Ironclad"] = 0.4f,
		["Silent"] = 0.07f,
		["Defect"] = 0.4f,
		["Necrobinder"] = 0.4f,
		["Byrd"] = 0.4f,
		["Osty"] = 0.4f,
		["Regent"] = 0.4f
	};

	public const float DEFAULT_MULTI_HIT_EXTRA_DELAY = 0.8f;

	public static readonly Dictionary<string, float> _delayBeforeTeleportByCharacter = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

	public const float DEFAULT_DELAY_BEFORE_TELEPORT = 0f;

	public const float ALTERNATE_OFFSET = 150f;

	public const float SILENT_SWITCH_DURATION = 0.15f;

	public const string DASHED_META_KEY = "__MeleeAttack_dashed";

	public const string ORIG_COLOR_META_KEY = "__MeleeAttack_orig_color";

	public const int HIGH_Z_INDEX = 5;

	public const float DEFAULT_MONSTER_DELAY = 0.6f;

	public const float DEFAULT_MONSTER_DISTANCE = 250f;

	public static readonly HashSet<string> _monsterNoTeleportAll = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"EYE_WITH_TEETH", "CROSSBOW_RUBY_RAIDER", "CUBEX_CONSTRUCT", "FUZZY_WURM_CRAWLER", "SNAPPING_JAXFRUIT", "FLYCONID", "BYGONE_EFFIGY", "KIN_PRIEST", "LIVING_FOG", "DECIMILLIPEDE_SEGMENT_FRONT",
		"DECIMILLIPEDE_SEGMENT_MIDDLE", "DECIMILLIPEDE_SEGMENT_BACK", "TURRET_OPERATOR", "MAGI_KNIGHT", "QUEEN"
	};

	public static readonly HashSet<string> _monsterNoTeleportAttacks = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "*|attack_ranged", "*|AttackFlame" };

	public static readonly Dictionary<string, float> _monsterDistanceByTrigger = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

	public static readonly Dictionary<string, float> _monsterDistanceByMonster = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

	internal static readonly ConcurrentDictionary<NCreature, CancellationTokenSource> _bodySlamDashCts = new ConcurrentDictionary<NCreature, CancellationTokenSource>();

	internal static readonly ConcurrentDictionary<NCreature, Task> _uppercutSequenceTasks = new ConcurrentDictionary<NCreature, Task>();

	internal static readonly ConcurrentDictionary<NCreature, Vector2Wrapper> _initialScales = new ConcurrentDictionary<NCreature, Vector2Wrapper>();

	private static readonly ConcurrentDictionary<NCreature, ColorWrapper> _originalColors = new ConcurrentDictionary<NCreature, ColorWrapper>();

	internal static readonly ConcurrentDictionary<NCreature, IntWrapper> _originalZIndices = new ConcurrentDictionary<NCreature, IntWrapper>();

	internal static readonly ConcurrentDictionary<NCreature, CancellationTokenSource> _positionLockCtsTable = new ConcurrentDictionary<NCreature, CancellationTokenSource>();

	internal static readonly ConcurrentDictionary<NCreature, Vector2Wrapper> _visualInitialScales = new ConcurrentDictionary<NCreature, Vector2Wrapper>();

	internal static readonly ConcurrentDictionary<NCreature, Vector2Wrapper> _blockIconInitialPositions = new ConcurrentDictionary<NCreature, Vector2Wrapper>();

	internal static readonly ConcurrentDictionary<NCreature, CancellationTokenSource> _whirlwindFlipCts = new ConcurrentDictionary<NCreature, CancellationTokenSource>();

	internal static readonly AsyncLocal<AttackCommand?> _currentAttack = new AsyncLocal<AttackCommand>();

	internal static readonly AsyncLocal<bool> _isProxyRunning = new AsyncLocal<bool>();

	internal static readonly AsyncLocal<int> _currentHitIndex = new AsyncLocal<int>();

	internal static CancellationTokenSource? _positionLockCts;

	internal static readonly ConcurrentDictionary<NCreature, CancellationTokenSource> _returnCtsTable = new ConcurrentDictionary<NCreature, CancellationTokenSource>();

	internal static readonly AsyncLocal<bool> IsAttackInProgress = new AsyncLocal<bool>();

	internal static int _attackExecNesting = 0;

	internal static readonly MethodInfo? _originalTriggerAnim = typeof(CreatureCmd).GetMethod("TriggerAnim", new Type[3]
	{
		typeof(Creature),
		typeof(string),
		typeof(float)
	});

	internal static readonly ConcurrentDictionary<NCreature, AttackSequenceState> _attackStates = new ConcurrentDictionary<NCreature, AttackSequenceState>();

	private static FieldInfo _hitCountField;

	private static MethodInfo _getPossibleTargetsMethod;

	private static PropertyInfo _defaultScaleProp;

	private static void PlayDefectUpgradedEffect(Creature attacker, List<Creature> targets)
	{
	}

	private static void PlayIroncladUpgradedEffect(Creature attacker, List<Creature> targets)
	{
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		AttackCommand value = _currentAttack.Value;
		if (value == null)
		{
			return;
		}
		AbstractModel modelSource = value.ModelSource;
		CardModel val = (CardModel)(object)((modelSource is CardModel) ? modelSource : null);
		if (val == null)
		{
			return;
		}
		CardPoolModel pool = val.Pool;
		if (pool == null || !string.Equals(pool.Title, "Ironclad", StringComparison.OrdinalIgnoreCase) || !val.IsUpgraded)
		{
			return;
		}
		foreach (Creature target in targets)
		{
			if (target != null && target.IsAlive)
			{
				Vector2 visualCenter = GetVisualCenter(target);
				if (visualCenter != Vector2.Zero)
				{
					PlayVfxAtPosition("vfx/vfx_attack_blunt", visualCenter);
				}
			}
		}
	}

	public static bool IsWhirlwindCard(string cardId)
	{
		return !string.IsNullOrEmpty(cardId) && cardId.Equals("WHIRLWIND", StringComparison.OrdinalIgnoreCase);
	}

	internal static void StartWhirlwindFlip(NCreature node)
	{
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return;
		}
		if (_whirlwindFlipCts.ContainsKey(node))
		{
			GD.Print("[MeleeAttack][Whirlwind] 翻转循环已存在，跳过重启");
			return;
		}
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		if (!_whirlwindFlipCts.TryAdd(node, cancellationTokenSource))
		{
			cancellationTokenSource.Dispose();
			return;
		}
		FlipLoopAsync(node, cancellationTokenSource.Token);
		PlayWhirlwindVfx(node);
		FloatingHitEffect.StartWhirlwindOrbit(node);
		GD.Print($"[MeleeAttack][Whirlwind] 启动翻转循环（间隔 {0.1f}s）");
	}

	internal static void StopWhirlwindFlip(NCreature node)
	{
		if (node != null && _whirlwindFlipCts.TryRemove(node, out var value))
		{
			try
			{
				value?.Cancel();
			}
			catch
			{
			}
			try
			{
				value?.Dispose();
			}
			catch
			{
			}
			StopWhirlwindVfx(node);
			FloatingHitEffect.StopWhirlwindOrbit(node);
			GD.Print("[MeleeAttack][Whirlwind] 停止翻转循环");
		}
	}

	private static async Task FlipLoopAsync(NCreature node, CancellationToken token)
	{
		while (!token.IsCancellationRequested && node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			Node2D visual = node.Body;
			if (visual == null || !GodotObject.IsInstanceValid((GodotObject)(object)visual))
			{
				break;
			}
			Vector2 scale = visual.Scale;
			visual.Scale = new Vector2(0f - scale.X, scale.Y);
			if (!(await WaitSecondsAsync(0.1f, token)))
			{
				break;
			}
		}
	}

	private static void PlayWhirlwindVfx(NCreature creatureNode)
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		if (creatureNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
		{
			return;
		}
		if (_whirlwindVfx.ContainsKey(creatureNode))
		{
			GD.Print("[WhirlwindVfx] 特效已存在，跳过");
			return;
		}
		PackedScene val = GD.Load<PackedScene>("res://scenes/旋风斩.tscn");
		if (val == null)
		{
			GD.PrintErr("[WhirlwindVfx] 无法加载特效: res://scenes/旋风斩.tscn");
			return;
		}
		Node2D val2 = val.Instantiate<Node2D>((GenEditState)0);
		if (val2 != null)
		{
			((Node)creatureNode).AddChild((Node)(object)val2, false, (InternalMode)0);
			Vector2 val3 = TargetCenter.GetCenter(creatureNode);
			if (val3 == Vector2.Zero)
			{
				val3 = ((Control)creatureNode).GlobalPosition;
			}
			val2.GlobalPosition = val3;
			((CanvasItem)val2).ZIndex = 3;
			((CanvasItem)val2).ZAsRelative = true;
			Node2D body = creatureNode.Body;
			if (body != null && GodotObject.IsInstanceValid((GodotObject)(object)body))
			{
				float num = ((body.Scale.X < 0f) ? (-1f) : 1f);
				float num2 = Mathf.Abs(val2.Scale.X);
				float y = val2.Scale.Y;
				val2.Scale = new Vector2(num2 * num, y);
			}
			float num3 = ((CanvasItem)val2).Modulate.A;
			if (num3 <= 0f)
			{
				num3 = 1f;
			}
			((CanvasItem)val2).Modulate = new Color(((CanvasItem)val2).Modulate.R, ((CanvasItem)val2).Modulate.G, ((CanvasItem)val2).Modulate.B, 0f);
			Tween val4 = ((Node)val2).CreateTween();
			val4.SetTrans((TransitionType)0);
			val4.SetEase((EaseType)2);
			val4.TweenProperty((GodotObject)(object)val2, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(num3), 0.20000000298023224);
			_whirlwindVfx[creatureNode] = val2;
			GD.Print($"[WhirlwindVfx] 播放旋风斩特效：{((Node)creatureNode).Name}，z相对={3}，渐入 {0.2f}s");
		}
	}

	private static void StopWhirlwindVfx(NCreature creatureNode)
	{
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		if (creatureNode == null || !_whirlwindVfx.TryRemove(creatureNode, out var vfx) || vfx == null || !GodotObject.IsInstanceValid((GodotObject)(object)vfx))
		{
			return;
		}
		Tween val = ((Node)vfx).CreateTween();
		val.SetTrans((TransitionType)0);
		val.SetEase((EaseType)2);
		val.TweenProperty((GodotObject)(object)vfx, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), 0.20000000298023224);
		val.TweenCallback(Callable.From((Action)delegate
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)vfx))
			{
				((Node)vfx).QueueFree();
			}
		}));
		GD.Print($"[WhirlwindVfx] 停止旋风斩特效：{((Node)creatureNode).Name}，渐出 {0.2f}s");
	}

	private static Vector2 GetBoundsCenter(NCreature creatureNode)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		if (creatureNode == null)
		{
			return Vector2.Zero;
		}
		if (creatureNode.Hitbox != null)
		{
			Vector2 globalPosition = creatureNode.Hitbox.GlobalPosition;
			Vector2 size = creatureNode.Hitbox.Size;
			if (size.X > 5f && size.Y > 5f)
			{
				return globalPosition + size * 0.5f;
			}
		}
		if (creatureNode.Body != null)
		{
			Node2D body = creatureNode.Body;
			Vector2 val = Vector2.One * 150f;
			Sprite2D val2 = (Sprite2D)(object)((body is Sprite2D) ? body : null);
			if (val2 != null && val2.Texture != null)
			{
				val = val2.Texture.GetSize() * ((Node2D)val2).Scale;
			}
			else
			{
				AnimatedSprite2D val3 = (AnimatedSprite2D)(object)((body is AnimatedSprite2D) ? body : null);
				if (val3 != null && val3.SpriteFrames != null)
				{
					Texture2D frameTexture = val3.SpriteFrames.GetFrameTexture(val3.Animation, 0);
					if (frameTexture != null)
					{
						val = frameTexture.GetSize() * ((Node2D)val3).Scale;
					}
				}
			}
			Vector2 val4 = body.GlobalPosition - val * 0.5f;
			return val4 + val * 0.5f;
		}
		return ((Control)creatureNode).GlobalPosition;
	}

	private static void PlayNecroAttackEffect()
	{
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Expected O, but got Unknown
		AttackCommand value = _currentAttack.Value;
		if (value == null)
		{
			return;
		}
		List<Creature> aliveTargets = GetAliveTargets(value);
		if (aliveTargets.Count == 0)
		{
			return;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return;
		}
		foreach (Creature item in aliveTargets)
		{
			NCreature creatureNode = instance.GetCreatureNode(item);
			if (creatureNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
			{
				continue;
			}
			Vector2 boundsCenter = GetBoundsCenter(creatureNode);
			if (boundsCenter == Vector2.Zero)
			{
				continue;
			}
			PackedScene necroBuffScene = _necroBuffScene;
			Node vfx = ((necroBuffScene != null) ? necroBuffScene.Instantiate((GenEditState)0) : null);
			if (vfx == null)
			{
				continue;
			}
			Node obj = vfx;
			Node2D val = (Node2D)(object)((obj is Node2D) ? obj : null);
			if (val != null)
			{
				val.GlobalPosition = boundsCenter;
				((CanvasItem)val).ZIndex = 0;
				((CanvasItem)val).ZAsRelative = false;
			}
			SetMouseTransparentNecro(vfx);
			StartParticlesNecro(vfx);
			Control val2 = (Control)(((object)instance.CombatVfxContainer) ?? ((object)instance));
			((Node)val2).AddChild(vfx, false, (InternalMode)0);
			Timer val3 = new Timer();
			val3.WaitTime = 2.0;
			val3.OneShot = true;
			val3.Timeout += delegate
			{
				if (GodotObject.IsInstanceValid((GodotObject)(object)vfx))
				{
					vfx.QueueFree();
				}
			};
			vfx.AddChild((Node)(object)val3, false, (InternalMode)0);
			val3.Start(-1.0);
		}
	}

	private static void SetMouseTransparentNecro(Node node)
	{
		Control val = (Control)(object)((node is Control) ? node : null);
		if (val != null)
		{
			val.MouseFilter = (MouseFilterEnum)2;
		}
		foreach (Node child in node.GetChildren(false))
		{
			SetMouseTransparentNecro(child);
		}
	}

	private static void StartParticlesNecro(Node node)
	{
		PropertyInfo property = ((object)node).GetType().GetProperty("Emitting");
		if (property != null && property.CanWrite)
		{
			property.SetValue(node, true);
		}
		foreach (Node child in node.GetChildren(false))
		{
			StartParticlesNecro(child);
		}
	}

	internal static async Task HandleDefectDash(NCreature rootNode, Vector2 targetPos, Vector2? facingReferencePoint = null)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		await PerformDashWithStretch(rootNode, targetPos, 0.25f, 3f, 6f, 0.75f, applyFacing: true, facingReferencePoint);
	}

	internal static async Task HandleIroncladDash(NCreature rootNode, Vector2 targetPos, Vector2? facingReferencePoint = null)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		await PerformDashWithStretch(rootNode, targetPos, 0.2f, 2f, 6f, 0.35f, applyFacing: true, facingReferencePoint);
	}

	internal static async Task HandleIroncladHeavyDash(NCreature rootNode, Vector2 targetPos, Vector2? facingReferencePoint = null)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		if (SettingsUI.IsIroncladHeavyEnabled())
		{
			await ArcMove(rootNode, targetPos, 0.2f, facingReferencePoint);
		}
		else
		{
			await HandleIroncladDash(rootNode, targetPos, facingReferencePoint);
		}
	}

	private static async Task ArcMove(NCreature rootNode, Vector2 targetPos, float duration, Vector2? facingReferencePoint = null)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		Vector2 startPos = ((Control)rootNode).GlobalPosition;
		float distance = ((Vector2)(ref startPos)).DistanceTo(targetPos);
		Vector2 val;
		Vector2 moveDir;
		if (facingReferencePoint.HasValue)
		{
			val = facingReferencePoint.Value - targetPos;
			moveDir = ((Vector2)(ref val)).Normalized();
		}
		else
		{
			val = targetPos - startPos;
			moveDir = ((Vector2)(ref val)).Normalized();
		}
		if (((Vector2)(ref moveDir)).LengthSquared() < 0.001f)
		{
			moveDir = Vector2.Right;
		}
		float maxHeight = distance * 0.3f;
		if (distance < 1f)
		{
			((Control)rootNode).GlobalPosition = targetPos;
			return;
		}
		Node2D visual = rootNode.Body;
		if (visual != null && GodotObject.IsInstanceValid((GodotObject)(object)visual))
		{
			ApplyFacingToVisual(rootNode, visual, moveDir);
		}
		Tween tween = ((Node)rootNode).CreateTween();
		TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
		tween.TweenMethod(Callable.From<float>((Action<float>)delegate(float t)
		{
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
			float num = Mathf.Clamp(t / duration, 0f, 1f);
			float easedProgress = GetEasedProgress(num);
			Vector2 val2 = ((Vector2)(ref startPos)).Lerp(targetPos, easedProgress);
			float num2 = 0.5f;
			float num3 = ((!(num <= num2)) ? (maxHeight * (1f - Mathf.Pow((num - num2) / (1f - num2), 2f))) : (maxHeight * (1f - Mathf.Pow((num - num2) / num2, 2f))));
			num3 = Mathf.Max(num3, 0f);
			((Control)rootNode).GlobalPosition = new Vector2(val2.X, val2.Y - num3);
		}), Variant.op_Implicit(0f), Variant.op_Implicit(duration), (double)duration);
		tween.Finished += delegate
		{
			tcs.SetResult(result: true);
		};
		await tcs.Task;
		((Control)rootNode).GlobalPosition = targetPos;
	}

	internal static async Task<bool> WaitTweenOrTimeout(Tween tween, Node node, float timeoutSeconds)
	{
		if (tween == null || node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return false;
		}
		TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
		tween.Finished += delegate
		{
			tcs.TrySetResult(result: true);
		};
		SceneTree tree = node.GetTree();
		if (tree != null && timeoutSeconds > 0f)
		{
			SceneTreeTimer timer = tree.CreateTimer((double)timeoutSeconds, true, false, false);
			timer.Timeout += delegate
			{
				tcs.TrySetResult(result: false);
			};
		}
		else
		{
			Task.Delay((int)(timeoutSeconds * 1000f)).ContinueWith((Task _) => tcs.TrySetResult(result: false));
		}
		await tcs.Task;
		return GodotObject.IsInstanceValid((GodotObject)(object)node) && node.IsInsideTree();
	}

	internal static float GetEasedProgress(float t, float splitTime = 0.625f, float splitProgress = 0.2f)
	{
		t = Mathf.Clamp(t, 0f, 1f);
		if (t <= splitTime)
		{
			return t / splitTime * splitProgress;
		}
		return splitProgress + (t - splitTime) / (1f - splitTime) * (1f - splitProgress);
	}

	internal static float GetStretchFactor(float progress, float peakStrength = 4.5f, float exponent = 4f, float peakProgress = 0.7f)
	{
		progress = Mathf.Clamp(progress, 0f, 1f);
		if (progress <= peakProgress)
		{
			float num = progress / peakProgress;
			float num2 = Mathf.Pow(num, exponent);
			return 1f + (peakStrength - 0.8f) * num2;
		}
		float num3 = (progress - peakProgress) / (1f - peakProgress);
		float num4 = 1f - num3;
		return 1f + (peakStrength - 1f) * num4;
	}

	public static void ApplyFacingToVisual(NCreature node, Node2D visual, Vector2 direction)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		if (node != null && visual != null && GodotObject.IsInstanceValid((GodotObject)(object)visual))
		{
			float num = Math.Sign(direction.X);
			if (Math.Abs(direction.X) < 0.001f)
			{
				num = 1f;
			}
			visual.Scale = new Vector2(Math.Abs(visual.Scale.X) * num, visual.Scale.Y);
		}
	}

	public static void RecordVisualInitialScale(NCreature node, Node2D visualNode)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		if (node != null && visualNode != null && GodotObject.IsInstanceValid((GodotObject)(object)visualNode) && !_visualInitialScales.ContainsKey(node))
		{
			_visualInitialScales[node] = new Vector2Wrapper(visualNode.Scale);
		}
	}

	public static async Task PerformDashWithStretch(NCreature node, Vector2 targetPos, float duration = 0.2f, float peakStrength = 2f, float exponent = 6f, float peakProgress = 0.7f, bool applyFacing = true, Vector2? facingReferencePoint = null)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return;
		}
		Node2D visual = node.Body;
		if (visual == null || !GodotObject.IsInstanceValid((GodotObject)(object)visual))
		{
			((Control)node).GlobalPosition = targetPos;
			return;
		}
		Vector2 val;
		Vector2 dir;
		if (facingReferencePoint.HasValue)
		{
			val = facingReferencePoint.Value - targetPos;
			dir = ((Vector2)(ref val)).Normalized();
		}
		else
		{
			val = targetPos - ((Control)node).GlobalPosition;
			dir = ((Vector2)(ref val)).Normalized();
		}
		if (((Vector2)(ref dir)).LengthSquared() < 0.001f)
		{
			dir = Vector2.Right;
		}
		Vector2 origScale = visual.Scale;
		Vector2 absScale = new Vector2(Math.Abs(origScale.X), Math.Abs(origScale.Y));
		float sign = (applyFacing ? Math.Sign(dir.X) : Math.Sign(origScale.X));
		if (Math.Abs(dir.X) < 0.001f)
		{
			sign = 1f;
		}
		if (applyFacing)
		{
			visual.Scale = new Vector2(absScale.X * sign, absScale.Y);
		}
		Vector2 startPos = ((Control)node).GlobalPosition;
		Toutou._stretchOriginalScales.AddOrUpdate(node, new Vector2Wrapper(absScale));
		Tween tween = ((Node)node).CreateTween();
		tween.SetParallel(false);
		tween.SetTrans((TransitionType)0);
		tween.SetEase((EaseType)2);
		tween.TweenMethod(Callable.From<float>((Action<float>)delegate(float t)
		{
			//IL_005a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_00af: Unknown result type (might be due to invalid IL or missing references)
			if (GodotObject.IsInstanceValid((GodotObject)(object)node) && GodotObject.IsInstanceValid((GodotObject)(object)visual))
			{
				float num = t / duration;
				float easedProgress = GetEasedProgress(num);
				((Control)node).GlobalPosition = ((Vector2)(ref startPos)).Lerp(targetPos, easedProgress);
				float stretchFactor = GetStretchFactor(num, peakStrength, exponent, peakProgress);
				float num2 = absScale.X * stretchFactor;
				float y = absScale.Y;
				visual.Scale = new Vector2(num2 * sign, y);
			}
		}), Variant.op_Implicit(0f), Variant.op_Implicit(duration), (double)duration);
		bool completed = await WaitTweenOrTimeout(tween, (Node)(object)node, duration + 0.5f);
		Toutou._stretchOriginalScales.Remove(node);
		if (completed && GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)visual))
			{
				visual.Scale = new Vector2(absScale.X * sign, absScale.Y);
			}
			((Control)node).GlobalPosition = targetPos;
		}
	}

	public static void PerformTeleport(NCreature node, Vector2 targetPos, Vector2? facingReferencePoint = null)
	{
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return;
		}
		Node2D body = node.Body;
		if (body != null && GodotObject.IsInstanceValid((GodotObject)(object)body))
		{
			Vector2 val;
			Vector2 direction;
			if (facingReferencePoint.HasValue)
			{
				val = facingReferencePoint.Value - targetPos;
				direction = ((Vector2)(ref val)).Normalized();
			}
			else
			{
				val = targetPos - ((Control)node).GlobalPosition;
				direction = ((Vector2)(ref val)).Normalized();
			}
			if (((Vector2)(ref direction)).LengthSquared() < 0.001f)
			{
				direction = Vector2.Right;
			}
			ApplyFacingToVisual(node, body, direction);
		}
		((Control)node).GlobalPosition = targetPos;
	}

	public static async Task PerformFirstHitMovement(NCreature node, Vector2 homePos, Vector2 visualCenter, Vector2 dir, float offset = 150f)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		Vector2 frontPos = visualCenter - dir * offset;
		await PerformSilentRetreat(node, frontPos, homePos);
		Vector2 overrunPos = visualCenter + dir * offset;
		await PerformSilentDash(node, overrunPos);
	}

	public static async Task PerformSilentRetreat(NCreature node, Vector2 targetPos, Vector2 homePos, float distance = 120f, float duration = 0.1f, float fadeEnd = 0.05f)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return;
		}
		Node2D visual = node.Body;
		if (visual == null || !GodotObject.IsInstanceValid((GodotObject)(object)visual))
		{
			((Control)node).GlobalPosition = targetPos;
			return;
		}
		float originalAlpha = ((CanvasItem)visual).Modulate.A;
		Vector2 val = homePos - targetPos;
		Vector2 direction = ((Vector2)(ref val)).Normalized();
		if (((Vector2)(ref direction)).LengthSquared() < 0.001f)
		{
			direction = Vector2.Right;
		}
		Vector2 retreatPos = homePos + direction * distance;
		Tween tween = ((Node)node).CreateTween();
		tween.SetParallel(true);
		tween.SetTrans((TransitionType)0);
		tween.SetEase((EaseType)2);
		tween.TweenProperty((GodotObject)(object)node, NodePath.op_Implicit("global_position"), Variant.op_Implicit(retreatPos), (double)duration);
		tween.TweenProperty((GodotObject)(object)visual, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(fadeEnd), (double)duration);
		if (await WaitTweenOrTimeout(tween, (Node)(object)node, duration + 0.5f) && GodotObject.IsInstanceValid((GodotObject)(object)node) && GodotObject.IsInstanceValid((GodotObject)(object)visual))
		{
			((Control)node).GlobalPosition = targetPos;
			((CanvasItem)visual).Modulate = new Color(((CanvasItem)visual).Modulate, originalAlpha);
		}
	}

	public static async Task PerformSilentDash(NCreature node, Vector2 targetPos)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		if (node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			float duration = 0.12f;
			Tween tween = ((Node)node).CreateTween();
			tween.SetParallel(false);
			tween.SetTrans((TransitionType)0);
			tween.SetEase((EaseType)2);
			tween.TweenProperty((GodotObject)(object)node, NodePath.op_Implicit("global_position"), Variant.op_Implicit(targetPos), (double)duration);
			if (await WaitTweenOrTimeout(tween, (Node)(object)node, duration + 0.5f) && GodotObject.IsInstanceValid((GodotObject)(object)node))
			{
				((Control)node).GlobalPosition = targetPos;
			}
		}
	}

	internal static void PlayGrandFinaleEffect(Creature attacker, List<Creature> targets)
	{
		AttackCommand value = _currentAttack.Value;
		if (value == null)
		{
			return;
		}
		AbstractModel modelSource = value.ModelSource;
		CardModel val = (CardModel)(object)((modelSource is CardModel) ? modelSource : null);
		if (val == null)
		{
			return;
		}
		bool flag = false;
		bool flag2 = false;
		CardPoolModel pool = val.Pool;
		if (pool != null && string.Equals(pool.Title, "Silent", StringComparison.OrdinalIgnoreCase))
		{
			flag = val.IsUpgraded;
		}
		flag2 = (val.Tags.Contains((CardTag)5) || ((AbstractModel)val).Id.Entry.IndexOf("Shiv", StringComparison.OrdinalIgnoreCase) >= 0) && val.IsUpgraded;
		if (!flag && !flag2)
		{
			return;
		}
		foreach (Creature target in targets)
		{
			if (target != null && target.IsAlive)
			{
				VfxCmd.PlayOnCreatureCenter(target, "vfx/vfx_grand_finale_impact");
			}
		}
	}

	internal static bool IsAttackTriggerName(string triggerName)
	{
		if (string.IsNullOrEmpty(triggerName))
		{
			return false;
		}
		foreach (string attackTriggerPrefix in _attackTriggerPrefixes)
		{
			if (triggerName.Contains(attackTriggerPrefix, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	internal static float GetExtraBuffer(string charId)
	{
		if (!string.IsNullOrEmpty(charId) && _extraBufferByCharacter.TryGetValue(charId, out var value))
		{
			return value;
		}
		return 0.4f;
	}

	internal static float GetMultiHitExtraDelay(string charId)
	{
		if (!string.IsNullOrEmpty(charId) && _multiHitExtraDelayByCharacter.TryGetValue(charId, out var value))
		{
			return value;
		}
		return 0.8f;
	}

	internal static float GetDelayBeforeTeleport(string charId)
	{
		if (!string.IsNullOrEmpty(charId) && _delayBeforeTeleportByCharacter.TryGetValue(charId, out var value))
		{
			return value;
		}
		return 0f;
	}

	internal static bool IsMonsterTeleportDisabled(string monsterId, string triggerName)
	{
		if (string.IsNullOrEmpty(monsterId))
		{
			return false;
		}
		if (_monsterNoTeleportAll.Contains(monsterId))
		{
			return true;
		}
		if (!string.IsNullOrEmpty(triggerName) && triggerName.Equals("attack_flame", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (!string.IsNullOrEmpty(triggerName) && !IsAttackTriggerName(triggerName))
		{
			return true;
		}
		if (_monsterNoTeleportAttacks.Contains(monsterId + "|null"))
		{
			return true;
		}
		if (!string.IsNullOrEmpty(triggerName))
		{
			if (_monsterNoTeleportAttacks.Contains(monsterId + "|" + triggerName))
			{
				return true;
			}
			if (_monsterNoTeleportAttacks.Contains("*|" + triggerName))
			{
				return true;
			}
		}
		return false;
	}

	internal static float GetMonsterDelay(string monsterId, string triggerName)
	{
		return 0.6f;
	}

	internal static float GetMonsterDistance(string monsterId, string triggerName)
	{
		if (!string.IsNullOrEmpty(monsterId) && _monsterDistanceByMonster.TryGetValue(monsterId, out var value))
		{
			return value;
		}
		if (!string.IsNullOrEmpty(triggerName) && _monsterDistanceByTrigger.TryGetValue(triggerName, out var value2))
		{
			return value2;
		}
		return 250f;
	}

	internal static Task InvokeOriginalTriggerAnim(Creature creature, string triggerName, float waitTime)
	{
		_isProxyRunning.Value = true;
		try
		{
			if (_originalTriggerAnim != null)
			{
				return (Task)_originalTriggerAnim.Invoke(null, new object[3] { creature, triggerName, waitTime });
			}
			CreatureCmd.TriggerAnim(creature, triggerName, waitTime);
			return Task.CompletedTask;
		}
		finally
		{
			_isProxyRunning.Value = false;
		}
	}

	private static async Task DoRetreatThenTeleport(NCreature attackerNode, Node2D visualNode, Vector2 homePos, Vector2 finalPos, Vector2 facingDir, string logTag)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if (attackerNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)attackerNode))
		{
			return;
		}
		if (visualNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)visualNode))
		{
			((Control)attackerNode).GlobalPosition = finalPos;
			return;
		}
		float originalAlpha = ((CanvasItem)visualNode).Modulate.A;
		Vector2 val = homePos - finalPos;
		Vector2 direction = ((Vector2)(ref val)).Normalized();
		if (((Vector2)(ref direction)).LengthSquared() < 0.001f)
		{
			direction = Vector2.Right;
		}
		Vector2 retreatPos = homePos + direction * 120f;
		Tween tween = ((Node)attackerNode).CreateTween();
		tween.SetParallel(true);
		tween.SetTrans((TransitionType)0);
		tween.SetEase((EaseType)2);
		tween.TweenProperty((GodotObject)(object)attackerNode, NodePath.op_Implicit("global_position"), Variant.op_Implicit(retreatPos), 0.10000000149011612);
		tween.TweenProperty((GodotObject)(object)visualNode, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0.05f), 0.10000000149011612);
		if (await WaitTweenOrTimeout(tween, (Node)(object)attackerNode, 0.6f) && GodotObject.IsInstanceValid((GodotObject)(object)attackerNode) && GodotObject.IsInstanceValid((GodotObject)(object)visualNode))
		{
			((Control)attackerNode).GlobalPosition = finalPos;
			ApplyFacingToVisual(attackerNode, visualNode, facingDir);
			((CanvasItem)visualNode).Modulate = new Color(((CanvasItem)visualNode).Modulate, originalAlpha);
		}
	}

	private static async Task RunReturnDetached(NCreature attackerNode, Vector2 lockedHomePos, float extraBuffer, CancellationToken token, Node2D bodyNode, List<NCreature> targetNodesGeneral, bool isBodySlamFlag)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Task seqTask;
			bool hadUppercutSeq = _uppercutSequenceTasks.TryRemove(attackerNode, out seqTask);
			if (hadUppercutSeq)
			{
				try
				{
					await seqTask;
				}
				catch
				{
				}
			}
			if (hadUppercutSeq)
			{
				float delayForFall = extraBuffer;
				if (_attackStates.TryGetValue(attackerNode, out var st2))
				{
					delayForFall = extraBuffer + st2.PendingExtraDelay;
				}
				UppercutEffect.RunFallDuringDelayAsync(attackerNode, bodyNode, delayForFall);
			}
			await ReturnAfterDelay(attackerNode, lockedHomePos, extraBuffer, token, bodyNode, targetNodesGeneral, isBodySlamFlag);
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex3)
		{
			Exception ex = ex3;
			GD.PrintErr("[MeleeAttack] 归位任务异常: " + ex.Message);
		}
	}

	internal static async Task RunTeleportAttack(Creature attacker, string triggerName, float waitTime, List<Creature> targets, int hitIndex)
	{
		IsAttackInProgress.Value = true;
		try
		{
			AttackCommand command = _currentAttack.Value;
			AbstractModel val = ((command != null) ? command.ModelSource : null);
			CardModel card = (CardModel)(object)((val is CardModel) ? val : null);
			string cardId = ((card != null) ? ((AbstractModel)card).Id.Entry : null);
			triggerName = UppercutEffect.NormalizeTrigger(cardId, triggerName);
			string charId = GetCreatureId(attacker);
			if (string.IsNullOrEmpty(charId) && !string.IsNullOrEmpty(cardId))
			{
				if (cardId.IndexOf("byrd_swoop", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					charId = "Byrd";
				}
				else if (cardId.IndexOf("osty", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					charId = "Osty";
				}
			}
			if (string.IsNullOrEmpty(charId) && attacker != null)
			{
				string name = attacker.Name;
				if (name.IndexOf("byrd", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("异鸟", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					charId = "Byrd";
				}
				else if (name.IndexOf("osty", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("奥斯提", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					charId = "Osty";
				}
			}
			if (!SettingsUI.IsTeleportEnabledForCharacter(charId))
			{
				await InvokeOriginalTriggerAnim(attacker, triggerName, waitTime);
			}
			else
			{
				if (!TryGetCreatureNode(attacker, out var _, out var attackerNode))
				{
					return;
				}
				await MurderEffect.WaitIfUnstoppableAsync(attackerNode);
				bool isFirstHit = false;
				if (hitIndex == 0)
				{
					isFirstHit = !_attackStates.TryGetValue(attackerNode, out var existingState) || existingState.IsReturning;
				}
				AttackSequenceState existingState2;
				bool hasExistingState = _attackStates.TryGetValue(attackerNode, out existingState2) && !existingState2.IsReturning;
				AttackSequenceState state;
				Vector2 lockedHomePos;
				if (hasExistingState)
				{
					state = existingState2;
					lockedHomePos = state.LockedHomePos;
				}
				else
				{
					if (existingState2?.IsReturning ?? false)
					{
						if (existingState2.ReturnCompletion != null)
						{
							await existingState2.ReturnCompletion.Task;
						}
						_attackStates.TryRemove(attackerNode, out var _);
					}
					lockedHomePos = ((Control)attackerNode).GlobalPosition;
					state = GetOrCreateAttackState(attackerNode, lockedHomePos);
				}
				if (_returnCtsTable.TryRemove(attackerNode, out var oldCts))
				{
					oldCts?.Cancel();
					oldCts?.Dispose();
				}
				if (_bodySlamDashCts.TryRemove(attackerNode, out var oldDashCts))
				{
					oldDashCts?.Cancel();
					oldDashCts?.Dispose();
				}
				if (!hasExistingState)
				{
					_initialScales[attackerNode] = new Vector2Wrapper(((Control)attackerNode).Scale);
				}
				Xue.MakeHealthBarTransparent(attackerNode);
				RaiseLayer(attackerNode);
				bool isDefect = string.Equals(charId, "Defect", StringComparison.OrdinalIgnoreCase);
				bool isSilent = string.Equals(charId, "Silent", StringComparison.OrdinalIgnoreCase);
				bool isIronclad = string.Equals(charId, "Ironclad", StringComparison.OrdinalIgnoreCase);
				bool isBackstab = !string.IsNullOrEmpty(cardId) && cardId.Equals("BACKSTAB", StringComparison.OrdinalIgnoreCase);
				bool isBodySlam = !string.IsNullOrEmpty(cardId) && cardId.Equals("BODY_SLAM", StringComparison.OrdinalIgnoreCase);
				bool bodySlamEnabled = SettingsUI.IsBodySlamEnabled();
				List<NCreature> targetNodesGeneral = new List<NCreature>();
				foreach (Creature t in targets)
				{
					if (TryGetCreatureNode(t, out var _, out var node))
					{
						targetNodesGeneral.Add(node);
					}
					node = null;
				}
				if (targetNodesGeneral.Count == 0)
				{
					return;
				}
				if (targetNodesGeneral.Count > 0)
				{
					state.LastAttackTargetCenter = GetTargetRootCenter(targetNodesGeneral);
					state.HasLastAttackTarget = true;
				}
				MovementResult moveResult = await ExecuteMovement(attackerNode, charId, triggerName, isDefect, isSilent, isIronclad, isBackstab, targetNodesGeneral, lockedHomePos, hitIndex, cardId, isBodySlam, bodySlamEnabled, isFirstHit);
				bool lockEnabled = moveResult.LockEnabled;
				Task moveTask = moveResult.MoveTask;
				await PlayEffectsAndAnimation(attacker, targets, attackerNode, triggerName, waitTime, lockEnabled);
				if (moveTask != null && !moveTask.IsCompleted)
				{
					await moveTask;
				}
				float extraBuffer = SettingsUI.GetDelayForCharacter(charId);
				if (isIronclad && triggerName == "heavyAttack")
				{
					extraBuffer += SettingsUI.GetIroncladHeavyDelay();
				}
				CancellationTokenSource cts = new CancellationTokenSource();
				_returnCtsTable[attackerNode] = cts;
				bool isBodySlamFlag = isBodySlam && bodySlamEnabled;
				if (isBodySlamFlag)
				{
					Vector2 dashDir = state.BodySlamFacingDir;
					if (((Vector2)(ref dashDir)).LengthSquared() < 0.001f)
					{
						Vector2 targetCenter = GetTargetRootCenter(targetNodesGeneral);
						Vector2 val2 = targetCenter - ((Control)attackerNode).GlobalPosition;
						dashDir = ((Vector2)(ref val2)).Normalized();
						if (((Vector2)(ref dashDir)).LengthSquared() < 0.001f)
						{
							dashDir = Vector2.Right;
						}
					}
					CancellationTokenSource dashCts = new CancellationTokenSource();
					_bodySlamDashCts[attackerNode] = dashCts;
					Toutou.DashOutOfScreen(attackerNode, dashDir, dashCts.Token);
					dashDir = default(Vector2);
				}
				RunReturnDetached(bodyNode: attackerNode.Body, attackerNode: attackerNode, lockedHomePos: lockedHomePos, extraBuffer: extraBuffer, token: cts.Token, targetNodesGeneral: targetNodesGeneral, isBodySlamFlag: isBodySlamFlag);
				AttackTimeTerminator.StartTurnEndMonitoring(attacker);
			}
		}
		finally
		{
			IsAttackInProgress.Value = false;
		}
	}

	private static async Task<MovementResult> ExecuteMovement(NCreature attackerNode, string charId, string triggerName, bool isDefect, bool isSilent, bool isIronclad, bool isBackstab, List<NCreature> targetNodesGeneral, Vector2 homePos, int hitIndex, string cardId, bool isBodySlam, bool bodySlamEnabled, bool isFirstHit)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		Node2D visualNode = GetVisualNode(attackerNode);
		if (visualNode != null)
		{
			RecordVisualInitialScale(attackerNode, visualNode);
		}
		MovementResult result = default(MovementResult);
		result.TeleportPos = Vector2.Zero;
		result.LockEnabled = false;
		result.MoveTask = null;
		Vector2 currentPos = ((Control)attackerNode).GlobalPosition;
		if (isSilent && !string.IsNullOrEmpty(cardId) && _silentDisplacementExcludedCards.Contains(cardId))
		{
			result.TeleportPos = homePos;
			result.MoveTask = Task.CompletedTask;
			result.LockEnabled = false;
			return result;
		}
		Vector2 val;
		if (isSilent && targetNodesGeneral.Count > 1 && (!IsWhirlwindCard(cardId) || !SettingsUI.IsWhirlwindEnabled()))
		{
			NCreature farthestTarget = null;
			float maxDist = -1f;
			foreach (NCreature t in targetNodesGeneral)
			{
				if (t != null && GodotObject.IsInstanceValid((GodotObject)(object)t))
				{
					val = ((Control)attackerNode).GlobalPosition;
					float dist = ((Vector2)(ref val)).DistanceSquaredTo(((Control)t).GlobalPosition);
					if (dist > maxDist)
					{
						maxDist = dist;
						farthestTarget = t;
					}
				}
			}
			if (farthestTarget != null)
			{
				Vector2 targetCenter = ((Control)farthestTarget).GlobalPosition;
				val = targetCenter - ((Control)attackerNode).GlobalPosition;
				Vector2 horizontalDir = new Vector2((float)Mathf.Sign(((Vector2)(ref val)).Normalized().X), 0f);
				if (((Vector2)(ref horizontalDir)).LengthSquared() < 0.001f)
				{
					horizontalDir = Vector2.Left;
				}
				Vector2 targetPos = targetCenter + horizontalDir * 100f;
				targetPos.Y = ((Control)attackerNode).GlobalPosition.Y;
				val = targetCenter - ((Control)attackerNode).GlobalPosition;
				Vector2 facingDir = ((Vector2)(ref val)).Normalized();
				if (((Vector2)(ref facingDir)).LengthSquared() < 0.01f)
				{
					facingDir = Vector2.Left;
				}
				if (visualNode != null)
				{
					ApplyFacingToVisual(attackerNode, visualNode, facingDir);
				}
				YiShanEffect.PlayAt(attackerNode, targetPos);
				result.MoveTask = PerformDashWithStretch(attackerNode, targetPos);
				result.LockEnabled = false;
				return result;
			}
			result.MoveTask = Task.CompletedTask;
			return result;
		}
		if (isBodySlam && bodySlamEnabled)
		{
			Vector2 visualCenter = GetTargetRootCenter(targetNodesGeneral);
			val = visualCenter - currentPos;
			Vector2 facingDir2 = ((Vector2)(ref val)).Normalized();
			if (((Vector2)(ref facingDir2)).LengthSquared() < 0.01f)
			{
				facingDir2 = Vector2.Left;
			}
			if (_attackStates.TryGetValue(attackerNode, out var state))
			{
				state.BodySlamFacingDir = facingDir2;
			}
			if (visualNode != null)
			{
				ApplyFacingToVisual(attackerNode, visualNode, facingDir2);
			}
			Vector2 slamTargetPos = CalcTeleportPos(attackerNode, targetNodesGeneral, homePos, currentPos, triggerName, cardId);
			result.MoveTask = PerformDashWithStretch(attackerNode, slamTargetPos);
			result.LockEnabled = false;
			return result;
		}
		bool isUppercut = !string.IsNullOrEmpty(cardId) && cardId.Equals("UPPERCUT", StringComparison.OrdinalIgnoreCase);
		bool uppercutEnabled = SettingsUI.IsUppercutEnabled();
		if (isUppercut && uppercutEnabled)
		{
			Vector2 targetPos2 = CalcTeleportPos(attackerNode, targetNodesGeneral, homePos, currentPos, triggerName, cardId);
			Vector2 visualCenter2 = GetTargetRootCenter(targetNodesGeneral);
			val = visualCenter2 - currentPos;
			Vector2 facingDir3 = ((Vector2)(ref val)).Normalized();
			if (((Vector2)(ref facingDir3)).LengthSquared() < 0.01f)
			{
				facingDir3 = Vector2.Left;
			}
			if (visualNode != null && GodotObject.IsInstanceValid((GodotObject)(object)visualNode))
			{
				ApplyFacingToVisual(attackerNode, visualNode, facingDir3);
			}
			if (visualNode != null && GodotObject.IsInstanceValid((GodotObject)(object)visualNode) && _attackStates.TryGetValue(attackerNode, out var upState) && !upState.IsBackflipState)
			{
				upState.VisualOriginalPosition = visualNode.Position;
				upState.VisualOriginalRotation = visualNode.Rotation;
				upState.IsBackflipState = true;
			}
			Task seqTask = (isIronclad ? ((!(triggerName == "heavyAttack") || string.Equals(cardId, "BLUDGEON", StringComparison.OrdinalIgnoreCase)) ? IroncladDashThenLift(attackerNode, visualNode, targetPos2, heavy: false) : IroncladDashThenLift(attackerNode, visualNode, targetPos2, heavy: true)) : ((!isDefect) ? DashThenLift(attackerNode, visualNode, targetPos2) : DefectDashThenLift(attackerNode, visualNode, targetPos2)));
			_uppercutSequenceTasks[attackerNode] = seqTask;
			result.MoveTask = Task.CompletedTask;
			result.LockEnabled = false;
			return result;
		}
		bool isWhirlwind = IsWhirlwindCard(cardId);
		bool whirlwindEnabled = SettingsUI.IsWhirlwindEnabled();
		if (isWhirlwind && whirlwindEnabled)
		{
			Vector2 centerPos = GetTargetRootCenter(targetNodesGeneral);
			val = centerPos - currentPos;
			Vector2 facingDir4 = ((Vector2)(ref val)).Normalized();
			if (((Vector2)(ref facingDir4)).LengthSquared() < 0.01f)
			{
				facingDir4 = Vector2.Left;
			}
			if (visualNode != null)
			{
				ApplyFacingToVisual(attackerNode, visualNode, facingDir4);
			}
			result.MoveTask = PerformDashWithStretch(attackerNode, centerPos);
			StartWhirlwindFlip(attackerNode);
			result.LockEnabled = false;
			return result;
		}
		if (isFirstHit && isBackstab)
		{
			Vector2 visualCenter5 = GetTargetRootCenter(targetNodesGeneral);
			val = visualCenter5 - currentPos;
			Vector2 dir = ((Vector2)(ref val)).Normalized();
			if (((Vector2)(ref dir)).LengthSquared() < 0.01f)
			{
				dir = Vector2.Left;
			}
			Vector2 behindPos = visualCenter5 + dir * 150f;
			val = visualCenter5 - behindPos;
			Vector2 facingDir5 = ((Vector2)(ref val)).Normalized();
			if (((Vector2)(ref facingDir5)).LengthSquared() < 0.01f)
			{
				facingDir5 = Vector2.Left;
			}
			result.MoveTask = DoRetreatThenTeleport(attackerNode, visualNode, homePos, behindPos, facingDir5, $"背刺首击：后撤闪现到怪物背后 {behindPos}，同时翻转朝向");
			result.LockEnabled = false;
			return result;
		}
		if (isSilent)
		{
			Vector2 visualCenter4 = GetTargetRootCenter(targetNodesGeneral);
			val = visualCenter4 - currentPos;
			Vector2 dir2 = ((Vector2)(ref val)).Normalized();
			if (((Vector2)(ref dir2)).LengthSquared() < 0.01f)
			{
				dir2 = Vector2.Left;
			}
			if (visualNode != null)
			{
				ApplyFacingToVisual(attackerNode, visualNode, dir2);
			}
			bool isMurderFirstHit = isFirstHit && !string.IsNullOrEmpty(cardId) && cardId.Equals("MURDER", StringComparison.OrdinalIgnoreCase);
			if (isFirstHit && !isMurderFirstHit)
			{
				Vector2 targetPos5 = CalcTeleportPos(attackerNode, targetNodesGeneral, homePos, currentPos, triggerName, cardId);
				val = visualCenter4 - targetPos5;
				Vector2 facingDir6 = ((Vector2)(ref val)).Normalized();
				if (((Vector2)(ref facingDir6)).LengthSquared() < 0.01f)
				{
					facingDir6 = Vector2.Left;
				}
				result.MoveTask = DoRetreatThenTeleport(attackerNode, visualNode, homePos, targetPos5, facingDir6, $"[Silent] 普通首段：后撤闪现到 {targetPos5}");
				result.LockEnabled = false;
				return result;
			}
			if (isMurderFirstHit)
			{
				Vector2 behindPos2 = visualCenter4 + dir2 * 150f;
				val = visualCenter4 - behindPos2;
				Vector2 facingDir7 = ((Vector2)(ref val)).Normalized();
				if (((Vector2)(ref facingDir7)).LengthSquared() < 0.01f)
				{
					facingDir7 = Vector2.Left;
				}
				result.MoveTask = DoRetreatThenTeleport(attackerNode, visualNode, homePos, behindPos2, facingDir7, "[Silent] 谋杀首段：后撤闪现到背后，同时翻转朝向");
				result.LockEnabled = false;
				return result;
			}
			Vector2 horizontalDir2 = new Vector2((float)Mathf.Sign(dir2.X), 0f);
			if (((Vector2)(ref horizontalDir2)).LengthSquared() < 0.001f)
			{
				horizontalDir2 = Vector2.Left;
			}
			Vector2 overrunPos = visualCenter4 + horizontalDir2 * 150f;
			overrunPos.Y = ((Control)attackerNode).GlobalPosition.Y;
			result.MoveTask = PerformSilentDash(attackerNode, overrunPos);
			horizontalDir2 = default(Vector2);
			result.LockEnabled = false;
			return result;
		}
		if (isFirstHit)
		{
			Vector2 targetPos4 = CalcTeleportPos(attackerNode, targetNodesGeneral, homePos, currentPos, triggerName, cardId);
			if (isIronclad)
			{
				if (triggerName == "heavyAttack" && !string.Equals(cardId, "BLUDGEON", StringComparison.OrdinalIgnoreCase))
				{
					result.MoveTask = HandleIroncladHeavyDash(attackerNode, targetPos4);
				}
				else
				{
					result.MoveTask = HandleIroncladDash(attackerNode, targetPos4);
				}
			}
			else if (isDefect)
			{
				result.MoveTask = HandleDefectDash(attackerNode, targetPos4);
			}
			else
			{
				result.MoveTask = PerformDashWithStretch(attackerNode, targetPos4);
			}
		}
		else
		{
			Vector2 targetPos3 = CalcTeleportPos(attackerNode, targetNodesGeneral, homePos, currentPos, triggerName, cardId);
			Vector2 visualCenter3 = GetTargetRootCenter(targetNodesGeneral);
			PerformTeleport(attackerNode, targetPos3, visualCenter3);
			result.MoveTask = Task.CompletedTask;
		}
		result.LockEnabled = false;
		return result;
	}

	private static async Task IroncladDashThenLift(NCreature attackerNode, Node2D visualNode, Vector2 targetPos, bool heavy)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (attackerNode != null && GodotObject.IsInstanceValid((GodotObject)(object)attackerNode))
		{
			if (!heavy)
			{
				await HandleIroncladDash(attackerNode, targetPos);
			}
			else
			{
				await HandleIroncladHeavyDash(attackerNode, targetPos);
			}
			await UppercutEffect.DoLiftAsync(attackerNode, visualNode);
		}
	}

	private static async Task DefectDashThenLift(NCreature attackerNode, Node2D visualNode, Vector2 targetPos)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (attackerNode != null && GodotObject.IsInstanceValid((GodotObject)(object)attackerNode))
		{
			await HandleDefectDash(attackerNode, targetPos);
			await UppercutEffect.DoLiftAsync(attackerNode, visualNode);
		}
	}

	private static async Task DashThenLift(NCreature attackerNode, Node2D visualNode, Vector2 targetPos)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (attackerNode != null && GodotObject.IsInstanceValid((GodotObject)(object)attackerNode))
		{
			await PerformDashWithStretch(attackerNode, targetPos);
			await UppercutEffect.DoLiftAsync(attackerNode, visualNode);
		}
	}

	internal static async Task PlayEffectsAndAnimation(Creature attacker, List<Creature> targets, NCreature attackerNode, string triggerName, float waitTime, bool lockEnabled)
	{
		_isProxyRunning.Value = true;
		((GodotObject)attackerNode).SetMeta(StringName.op_Implicit("__MeleeAttack_attacking"), Variant.op_Implicit(true));
		try
		{
			if (_originalTriggerAnim != null)
			{
				Task animTask = (Task)_originalTriggerAnim.Invoke(null, new object[3] { attacker, triggerName, waitTime });
				await animTask;
			}
			else
			{
				CreatureCmd.TriggerAnim(attacker, triggerName, waitTime);
				await WaitSecondsAsync(waitTime, CancellationToken.None);
			}
		}
		finally
		{
			((GodotObject)attackerNode).RemoveMeta(StringName.op_Implicit("__MeleeAttack_attacking"));
			_isProxyRunning.Value = false;
			if (lockEnabled)
			{
				_positionLockCts?.Cancel();
				_positionLockCts = null;
			}
		}
	}

	public static void ClearAllSnapshots()
	{
		if (!IsAttackInProgress.Value)
		{
			_uppercutSequenceTasks.Clear();
			ClearAllAttackStates();
		}
	}

	internal static AttackSequenceState GetOrCreateAttackState(NCreature node, Vector2 homePos)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		if (_attackStates.TryGetValue(node, out var value))
		{
			return value;
		}
		Vector2 lockedHomeLocal = homePos;
		if (node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			lockedHomeLocal = ((Control)node).Position;
		}
		AttackSequenceState attackSequenceState = new AttackSequenceState
		{
			LockedHomePos = homePos,
			LockedHomeLocal = lockedHomeLocal,
			IsReturning = false,
			ReturnCts = new CancellationTokenSource(),
			ReturnCompletion = new TaskCompletionSource<bool>(),
			IsReturnDelayActive = false,
			ReturnDelayCts = null,
			BaseDelay = 0f,
			ExtraDelayAccumulated = 0f,
			ElapsedDelay = 0f,
			MaxTotalDelay = 3f,
			PendingExtraDelay = 0f,
			BodySlamFacingDir = Vector2.Zero,
			SkipFadeAndStretchOnReturn = false,
			LastAttackTargetCenter = Vector2.Zero,
			HasLastAttackTarget = false
		};
		if (_attackStates.TryAdd(node, attackSequenceState))
		{
			return attackSequenceState;
		}
		AttackSequenceState value2;
		return _attackStates.TryGetValue(node, out value2) ? value2 : attackSequenceState;
	}

	internal static bool HasActiveAttackState(NCreature node)
	{
		if (node == null)
		{
			return false;
		}
		if (_attackStates.TryGetValue(node, out var value))
		{
			return !value.IsReturning;
		}
		return false;
	}

	internal static bool HasAnyAttackState(NCreature node)
	{
		if (node == null)
		{
			return false;
		}
		return _attackStates.ContainsKey(node);
	}

	internal static bool IsReturningInProgress(NCreature node)
	{
		if (node == null)
		{
			return false;
		}
		if (_attackStates.TryGetValue(node, out var value))
		{
			return value.IsReturning;
		}
		return false;
	}

	internal static void MarkReturnStarted(NCreature node)
	{
		if (_attackStates.TryGetValue(node, out var value))
		{
			value.IsReturning = true;
			value.ReturnCts?.Cancel();
			value.ReturnCts = null;
		}
	}

	internal static void CompleteReturn(NCreature node)
	{
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		if (_attackStates.TryRemove(node, out var value))
		{
			value.ReturnCompletion?.TrySetResult(result: true);
			value.ReturnDelayCts?.Cancel();
			value.ReturnDelayCts?.Dispose();
			value.ReturnDelayCts = null;
			value.IsReturnDelayActive = false;
			value.ExtraDelayAccumulated = 0f;
			value.ElapsedDelay = 0f;
			value.PendingExtraDelay = 0f;
			value.SkipFadeAndStretchOnReturn = false;
			value.LastAttackTargetCenter = Vector2.Zero;
			value.HasLastAttackTarget = false;
		}
		StopWhirlwindFlip(node);
	}

	internal static void ClearAllAttackStates()
	{
		foreach (KeyValuePair<NCreature, AttackSequenceState> attackState in _attackStates)
		{
			attackState.Value.ReturnCts?.Cancel();
			attackState.Value.ReturnCompletion?.TrySetCanceled();
			attackState.Value.ReturnDelayCts?.Cancel();
			attackState.Value.ReturnDelayCts?.Dispose();
		}
		_attackStates.Clear();
		_blockIconInitialPositions.Clear();
		foreach (KeyValuePair<NCreature, CancellationTokenSource> whirlwindFlipCt in _whirlwindFlipCts)
		{
			try
			{
				whirlwindFlipCt.Value?.Cancel();
			}
			catch
			{
			}
			try
			{
				whirlwindFlipCt.Value?.Dispose();
			}
			catch
			{
			}
		}
		_whirlwindFlipCts.Clear();
	}

	public static void ClearHomePositions(NCreature node = null)
	{
		if (node == null)
		{
			ClearAllAttackStates();
		}
		else
		{
			_attackStates.TryRemove(node, out var _);
		}
	}

	internal static bool TryGetLockedHomePos(NCreature node, out Vector2 homePos)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		if (_attackStates.TryGetValue(node, out var value))
		{
			homePos = value.LockedHomePos;
			return true;
		}
		homePos = Vector2.Zero;
		return false;
	}

	internal static bool TryGetLockedHomeLocal(NCreature node, out Vector2 homeLocal)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		if (_attackStates.TryGetValue(node, out var value))
		{
			homeLocal = value.LockedHomeLocal;
			return true;
		}
		homeLocal = Vector2.Zero;
		return false;
	}

	internal static void RaiseLayer(NCreature node)
	{
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return;
		}
		if (!_originalZIndices.ContainsKey(node))
		{
			_originalZIndices[node] = new IntWrapper(((CanvasItem)node).ZIndex);
		}
		int num = int.MinValue;
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance != null)
		{
			foreach (NCreature creatureNode in instance.CreatureNodes)
			{
				if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode) && creatureNode != node && ((CanvasItem)creatureNode).ZIndex > num)
				{
					num = ((CanvasItem)creatureNode).ZIndex;
				}
			}
		}
		int zIndex = 5;
		if (num != int.MinValue)
		{
			zIndex = Math.Max(5, num + 1);
		}
		((CanvasItem)node).ZIndex = zIndex;
	}

	internal static void RestoreLayer(NCreature node)
	{
		if (node != null && GodotObject.IsInstanceValid((GodotObject)(object)node) && _originalZIndices.TryRemove(node, out var value))
		{
			((CanvasItem)node).ZIndex = value.Value;
		}
	}

	internal static Vector2 GetVisualTargetCenter(List<NCreature> targetNodes)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		if (targetNodes == null || targetNodes.Count == 0)
		{
			return Vector2.Zero;
		}
		if (targetNodes.Count == 1)
		{
			NCreature val = targetNodes[0];
			Node2D val2 = ((val != null) ? val.Body : null);
			if (val2 != null && GodotObject.IsInstanceValid((GodotObject)(object)val2))
			{
				return val2.GlobalPosition;
			}
			return (val != null) ? ((Control)val).GlobalPosition : Vector2.Zero;
		}
		Vector2 val3 = Vector2.Zero;
		int num = 0;
		foreach (NCreature targetNode in targetNodes)
		{
			if (targetNode != null && GodotObject.IsInstanceValid((GodotObject)(object)targetNode))
			{
				Node2D body = targetNode.Body;
				if (body != null && GodotObject.IsInstanceValid((GodotObject)(object)body))
				{
					val3 += body.GlobalPosition;
					num++;
				}
				else
				{
					val3 += ((Control)targetNode).GlobalPosition;
					num++;
				}
			}
		}
		if (num == 0)
		{
			return Vector2.Zero;
		}
		return val3 / (float)num;
	}

	internal static Vector2 GetTargetRootCenter(List<NCreature> targetNodes)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		if (targetNodes == null || targetNodes.Count == 0)
		{
			return Vector2.Zero;
		}
		Vector2 val = Vector2.Zero;
		int num = 0;
		foreach (NCreature targetNode in targetNodes)
		{
			if (targetNode != null && GodotObject.IsInstanceValid((GodotObject)(object)targetNode))
			{
				val += ((Control)targetNode).GlobalPosition;
				num++;
			}
		}
		return (num > 0) ? (val / (float)num) : Vector2.Zero;
	}

	internal static Vector2 CalcTeleportPos(NCreature node, List<NCreature> targetNodes, Vector2 homePos, Vector2 currentPos, string triggerName, string cardId)
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		string a = ((((node != null) ? node.Entity : null) != null) ? GetCreatureId(node.Entity) : null);
		float num = ((!string.IsNullOrEmpty(cardId) && cardId.Equals("BLUDGEON", StringComparison.OrdinalIgnoreCase) && string.Equals(a, "Ironclad", StringComparison.OrdinalIgnoreCase) && SettingsUI.IsBludgeonEnabled()) ? 450f : ((!(triggerName == "heavyAttack")) ? 150f : 300f));
		Vector2 targetRootCenter = GetTargetRootCenter(targetNodes);
		Vector2 val = targetRootCenter - currentPos;
		Vector2 val2 = ((Vector2)(ref val)).Normalized();
		if (((Vector2)(ref val2)).LengthSquared() < 0.01f)
		{
			val2 = Vector2.Left;
		}
		return targetRootCenter - val2 * num;
	}

	private static async Task<bool> WaitSecondsAsync(float seconds, CancellationToken token)
	{
		if (seconds <= 0f)
		{
			return true;
		}
		if (token.IsCancellationRequested)
		{
			return false;
		}
		MainLoop mainLoop = Engine.GetMainLoop();
		SceneTree tree = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
		if (tree == null)
		{
			try
			{
				await Task.Delay((int)(seconds * 1000f), token);
				return true;
			}
			catch (OperationCanceledException)
			{
				return false;
			}
		}
		TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
		SceneTreeTimer timer = tree.CreateTimer((double)seconds, true, false, false);
		timer.Timeout += delegate
		{
			tcs.TrySetResult(result: true);
		};
		while (!tcs.Task.IsCompleted)
		{
			if (token.IsCancellationRequested)
			{
				return false;
			}
			await ((GodotObject)tree).ToSignal((GodotObject)(object)tree, SignalName.ProcessFrame);
		}
		await tcs.Task;
		return !token.IsCancellationRequested;
	}

	internal static void AddReturnDelayWindow(float duration)
	{
		if (duration <= 0f)
		{
			return;
		}
		foreach (KeyValuePair<NCreature, AttackSequenceState> attackState in _attackStates)
		{
			AttackSequenceState value = attackState.Value;
			if (value != null && value.IsReturnDelayActive)
			{
				value.ExtraDelayAccumulated += duration;
			}
		}
	}

	internal static bool TryDecideFacingSign(NCreature self, AttackSequenceState state, float preferredSide, out float sign)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Invalid comparison between Unknown and I4
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		sign = 0f;
		if (self == null || state == null)
		{
			return false;
		}
		Vector2 lockedHomePos = state.LockedHomePos;
		if (Math.Abs(preferredSide) < 0.1f && state.HasLastAttackTarget)
		{
			float value = state.LastAttackTargetCenter.X - lockedHomePos.X;
			if (Math.Abs(value) > 1f)
			{
				preferredSide = Math.Sign(value);
			}
		}
		bool flag = false;
		bool flag2 = false;
		float num = 0f;
		int num2 = 0;
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance != null)
		{
			foreach (NCreature creatureNode in instance.CreatureNodes)
			{
				if (creatureNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode) || creatureNode == self)
				{
					continue;
				}
				Creature entity = creatureNode.Entity;
				if (entity == null || (int)entity.Side != 2 || !entity.IsAlive)
				{
					continue;
				}
				flag2 = true;
				float x = ((Control)creatureNode).GlobalPosition.X;
				num += x;
				num2++;
				if (Math.Abs(preferredSide) > 0.1f)
				{
					float value2 = x - lockedHomePos.X;
					if (Math.Abs(value2) > 1f && Math.Sign(value2) == Math.Sign(preferredSide))
					{
						flag = true;
					}
				}
			}
		}
		if (Math.Abs(preferredSide) > 0.1f && flag)
		{
			sign = preferredSide;
			return true;
		}
		if (flag2 && num2 > 0)
		{
			float num3 = num / (float)num2;
			float value3 = num3 - lockedHomePos.X;
			if (Math.Abs(value3) > 1f)
			{
				sign = Math.Sign(value3);
				return true;
			}
		}
		if (Math.Abs(preferredSide) > 0.1f)
		{
			sign = preferredSide;
			return true;
		}
		return false;
	}

	internal static async Task ExecuteReturnWithDelay(NCreature rootNode, Vector2 homePos, float baseDelay, CancellationToken cancelToken, Node2D visualNode = null, Vector2? visualOriginalPos = null, float? visualOriginalRot = null, bool isBodySlam = false)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		if (!_attackStates.TryGetValue(rootNode, out var state))
		{
			return;
		}
		state.BaseDelay = baseDelay;
		state.ExtraDelayAccumulated = state.PendingExtraDelay;
		state.PendingExtraDelay = 0f;
		state.IsReturnDelayActive = true;
		float localElapsed = 0f;
		while (true)
		{
			if (cancelToken.IsCancellationRequested)
			{
				state.IsReturnDelayActive = false;
				return;
			}
			if (Engine.TimeScale < 0.009999999776482582)
			{
				if (!(await WaitSecondsAsync(0.01f, cancelToken)))
				{
					state.IsReturnDelayActive = false;
					return;
				}
				continue;
			}
			float totalDelay = state.BaseDelay + state.ExtraDelayAccumulated;
			float remaining = totalDelay - localElapsed;
			if (remaining <= 0f || totalDelay > state.MaxTotalDelay)
			{
				break;
			}
			float waitStep = Math.Min(remaining, 0.05f);
			if (!(await WaitSecondsAsync(waitStep, cancelToken)))
			{
				state.IsReturnDelayActive = false;
				return;
			}
			localElapsed += waitStep;
		}
		state.IsReturnDelayActive = false;
		state.ReturnDelayCts = null;
		StopWhirlwindFlip(rootNode);
		if (rootNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)rootNode))
		{
			return;
		}
		bool skipFadeAndStretch = state.SkipFadeAndStretchOnReturn;
		MarkReturnStarted(rootNode);
		try
		{
			Toutou.CancelStretch(rootNode);
			if (isBodySlam)
			{
				if (_bodySlamDashCts.TryRemove(rootNode, out var dashCts))
				{
					dashCts?.Cancel();
					dashCts?.Dispose();
				}
				Toutou.FlashToLeftSide(rootNode, homePos);
			}
			if (_initialScales.TryRemove(rootNode, out var scaleWrapper))
			{
				((Control)rootNode).Scale = scaleWrapper.Value;
			}
			AttackSequenceState state2;
			if (visualNode != null && GodotObject.IsInstanceValid((GodotObject)(object)visualNode))
			{
				if (visualOriginalPos.HasValue)
				{
					visualNode.Position = visualOriginalPos.Value;
				}
				if (visualOriginalRot.HasValue)
				{
					visualNode.Rotation = visualOriginalRot.Value;
				}
			}
			else if (_attackStates.TryGetValue(rootNode, out state2) && state2.IsBackflipState)
			{
				Node2D vis2 = rootNode.Body;
				if (vis2 != null && GodotObject.IsInstanceValid((GodotObject)(object)vis2))
				{
					if (state2.VisualOriginalPosition.HasValue)
					{
						vis2.Position = state2.VisualOriginalPosition.Value;
					}
					if (state2.VisualOriginalRotation.HasValue)
					{
						vis2.Rotation = state2.VisualOriginalRotation.Value;
					}
					state2.IsBackflipState = false;
				}
			}
			if (!skipFadeAndStretch)
			{
				Toutou.StartFadeIn(rootNode, 0.7f, 0.7f);
			}
			await Toutou.StartReturnMove(rootNode, homePos, 0.2f, skipFadeAndStretch);
			if (!GodotObject.IsInstanceValid((GodotObject)(object)rootNode))
			{
				return;
			}
			((Control)rootNode).Position = state.LockedHomeLocal;
			Creature entity = rootNode.Entity;
			if (entity != null && (int)entity.Side == 1)
			{
				if (_visualInitialScales.TryRemove(rootNode, out var visScaleWrapper))
				{
					Node2D vis = GetVisualNode(rootNode);
					if (vis != null && GodotObject.IsInstanceValid((GodotObject)(object)vis))
					{
						float magnitudeX = Math.Abs(visScaleWrapper.Value.X);
						float preferredSide = Math.Sign(visScaleWrapper.Value.X);
						if (Math.Abs(visScaleWrapper.Value.X) < 0.001f)
						{
							preferredSide = 0f;
						}
						float currentSign = Math.Sign(vis.Scale.X);
						if (Math.Abs(vis.Scale.X) < 0.001f)
						{
							currentSign = 1f;
						}
						float targetSign = currentSign;
						if (TryDecideFacingSign(rootNode, state, preferredSide, out var decidedSign))
						{
							targetSign = decidedSign;
						}
						vis.Scale = new Vector2(magnitudeX * targetSign, vis.Scale.Y);
					}
				}
			}
			else
			{
				_visualInitialScales.TryRemove(rootNode, out var _);
			}
			RestoreLayer(rootNode);
			Xue.FadeInHealthBar(rootNode);
			((GodotObject)rootNode).RemoveMeta(StringName.op_Implicit("__MeleeAttack_dashed"));
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			GD.PrintErr("[MeleeAttack] 返回异常: " + ex.Message);
		}
		finally
		{
			CompleteReturn(rootNode);
		}
	}

	internal static async Task ReturnAfterDelay(NCreature rootNode, Vector2 homePos, float delay, CancellationToken token, Node2D bodyNode = null, List<NCreature> monsterNodes = null, bool isBodySlam = false)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		Vector2? visPos = null;
		float? visRot = null;
		if (_attackStates.TryGetValue(rootNode, out var state) && state.IsBackflipState)
		{
			visPos = state.VisualOriginalPosition;
			visRot = state.VisualOriginalRotation;
		}
		await ExecuteReturnWithDelay(rootNode, homePos, delay, token, bodyNode, visPos, visRot, isBodySlam);
	}

	internal static async Task PositionLockLoop(NCreature rootNode, Vector2 targetPos, CancellationToken token)
	{
	}//IL_0019: Unknown result type (might be due to invalid IL or missing references)
	//IL_001a: Unknown result type (might be due to invalid IL or missing references)


	internal static string GetCreatureId(Creature creature)
	{
		Player player = creature.Player;
		object result;
		if (player == null)
		{
			result = null;
		}
		else
		{
			CharacterModel character = player.Character;
			result = ((character != null) ? ((AbstractModel)character).Id.Entry : null);
		}
		return (string)result;
	}

	internal static int GetHitCount(AttackCommand command)
	{
		try
		{
			if (_hitCountField == null)
			{
				_hitCountField = typeof(AttackCommand).GetField("_hitCount", BindingFlags.Instance | BindingFlags.NonPublic);
				if (_hitCountField == null)
				{
					return 1;
				}
			}
			return (int)_hitCountField.GetValue(command);
		}
		catch
		{
			return 1;
		}
	}

	internal static bool TryGetCreatureNode(Creature creature, out NCombatRoom room, out NCreature node)
	{
		room = NCombatRoom.Instance;
		node = null;
		if (room == null || creature == null)
		{
			return false;
		}
		try
		{
			node = room.GetCreatureNode(creature);
			return node != null && GodotObject.IsInstanceValid((GodotObject)(object)node);
		}
		catch
		{
			return false;
		}
	}

	internal static List<Creature> GetAliveTargets(AttackCommand command)
	{
		try
		{
			if (_getPossibleTargetsMethod == null)
			{
				_getPossibleTargetsMethod = typeof(AttackCommand).GetMethod("GetPossibleTargets", BindingFlags.Instance | BindingFlags.NonPublic);
				if (_getPossibleTargetsMethod == null)
				{
					return new List<Creature>();
				}
			}
			return ((_getPossibleTargetsMethod.Invoke(command, null) is IReadOnlyList<Creature> source) ? source.Where((Creature c) => c != null && c.IsAlive).ToList() : null) ?? new List<Creature>();
		}
		catch
		{
			return new List<Creature>();
		}
	}

	internal static bool IsInstanceValid(NCreature node)
	{
		try
		{
			return node != null && GodotObject.IsInstanceValid((GodotObject)(object)node);
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsLocalPlayer(Creature creature)
	{
		if (creature == null || !creature.IsPlayer)
		{
			return false;
		}
		try
		{
			return LocalContext.IsMe(creature.Player);
		}
		catch
		{
			return false;
		}
	}

	internal static string GetCreatureIdFromNode(NCreature node)
	{
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		string text = StringName.op_Implicit(((Node)node).Name);
		if (text.Contains("Ironclad", StringComparison.OrdinalIgnoreCase))
		{
			return "Ironclad";
		}
		if (text.Contains("Silent", StringComparison.OrdinalIgnoreCase))
		{
			return "Silent";
		}
		if (text.Contains("Defect", StringComparison.OrdinalIgnoreCase))
		{
			return "Defect";
		}
		if (text.Contains("Necrobinder", StringComparison.OrdinalIgnoreCase))
		{
			return "Necrobinder";
		}
		if (text.Contains("Regent", StringComparison.OrdinalIgnoreCase))
		{
			return "Regent";
		}
		if (((GodotObject)node).HasMeta(StringName.op_Implicit("__MeleeAttack_charid")))
		{
			StringName obj = StringName.op_Implicit("__MeleeAttack_charid");
			Variant val = default(Variant);
			val = ((GodotObject)node).GetMeta(obj, val);
			return ((Variant)(ref val)).AsString();
		}
		return "";
	}

	internal static Node2D GetVisualNode(NCreature creatureNode)
	{
		if (creatureNode == null)
		{
			return null;
		}
		return creatureNode.Body;
	}

	public static Vector2 GetVisualCenter(Creature creature)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		if (creature == null)
		{
			return Vector2.Zero;
		}
		if (TryGetCreatureNode(creature, out var _, out var node) && node != null)
		{
			if (node.Body != null && GodotObject.IsInstanceValid((GodotObject)(object)node.Body))
			{
				return node.Body.GlobalPosition;
			}
			return ((Control)node).GlobalPosition;
		}
		return Vector2.Zero;
	}

	public static Vector2 GetVisualCenter(List<Creature> creatures)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		if (creatures == null || creatures.Count == 0)
		{
			return Vector2.Zero;
		}
		Vector2 val = Vector2.Zero;
		int num = 0;
		foreach (Creature creature in creatures)
		{
			if (creature != null && creature.IsAlive)
			{
				Vector2 visualCenter = GetVisualCenter(creature);
				if (visualCenter != Vector2.Zero)
				{
					val += visualCenter;
					num++;
				}
			}
		}
		return (num > 0) ? (val / (float)num) : Vector2.Zero;
	}

	public static void PlayVfxAtPosition(string vfxPath, Vector2 globalPos, int zIndex = 0)
	{
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Expected O, but got Unknown
		if (string.IsNullOrEmpty(vfxPath))
		{
			return;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return;
		}
		Control val = (Control)(((object)instance.CombatVfxContainer) ?? ((object)instance));
		PackedScene val2 = GD.Load<PackedScene>(vfxPath);
		if (val2 == null)
		{
			GD.PrintErr("[Utils] 无法加载特效场景: " + vfxPath);
			return;
		}
		Node2D node = val2.Instantiate<Node2D>((GenEditState)0);
		if (node == null)
		{
			return;
		}
		node.GlobalPosition = globalPos;
		((CanvasItem)node).ZIndex = zIndex;
		((CanvasItem)node).ZAsRelative = false;
		((Node)val).AddChild((Node)(object)node, false, (InternalMode)0);
		Timer val3 = new Timer();
		val3.WaitTime = 2.0;
		val3.OneShot = true;
		val3.Timeout += delegate
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)node))
			{
				((Node)node).QueueFree();
			}
		};
		((Node)node).AddChild((Node)(object)val3, false, (InternalMode)0);
		val3.Start(-1.0);
	}

	public static bool IsPlayerMainAttackInProgress()
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Invalid comparison between Unknown and I4
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return false;
		}
		foreach (NCreature creatureNode in instance.CreatureNodes)
		{
			if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
			{
				Creature entity = creatureNode.Entity;
				if (entity != null && (int)entity.Side == 1 && entity.IsPlayer && HasActiveAttackState(creatureNode))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static void ApplyOstyScaleOnly(NCreature node, float hp)
	{
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		if (node == null)
		{
			return;
		}
		NCreatureVisuals visuals = node.Visuals;
		if (visuals == null || !GodotObject.IsInstanceValid((GodotObject)(object)visuals))
		{
			return;
		}
		float num = Mathf.Lerp(Osty.ScaleRange.X, Osty.ScaleRange.Y, Mathf.Clamp(hp / 150f, 0f, 1f));
		float num2 = 1f;
		try
		{
			if (_defaultScaleProp == null)
			{
				_defaultScaleProp = ((object)visuals).GetType().GetProperty("DefaultScale", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			if (_defaultScaleProp != null)
			{
				num2 = (float)_defaultScaleProp.GetValue(visuals);
			}
		}
		catch
		{
			num2 = 1f;
		}
		((Node2D)visuals).Scale = Vector2.One * num * num2;
	}
}
