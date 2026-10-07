using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeleeAttack;

[HarmonyPatch]
public static class MurderSettlementEffect
{
	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class MurderBeforeDamagePatch
	{
		private static void Postfix(IRunState runState, ICombatState combatState, Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource)
		{
			//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_0166: Unknown result type (might be due to invalid IL or missing references)
			//IL_018c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0192: Invalid comparison between Unknown and I4
			//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b8: Invalid comparison between Unknown and I4
			//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_02c5: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
			//IL_034b: Unknown result type (might be due to invalid IL or missing references)
			//IL_035c: Unknown result type (might be due to invalid IL or missing references)
			//IL_037f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0386: Expected O, but got Unknown
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(85, 6);
			defaultInterpolatedStringHandler.AppendLiteral("[MurderDbg] Hook 命中: cardSource=");
			object obj;
			if (cardSource == null)
			{
				obj = null;
			}
			else
			{
				ModelId id = ((AbstractModel)cardSource).Id;
				obj = ((id != null) ? id.Entry : null);
			}
			if (obj == null)
			{
				obj = "null";
			}
			defaultInterpolatedStringHandler.AppendFormatted((string?)obj);
			defaultInterpolatedStringHandler.AppendLiteral(", ");
			defaultInterpolatedStringHandler.AppendLiteral("dealer=");
			defaultInterpolatedStringHandler.AppendFormatted(((dealer != null) ? dealer.Name : null) ?? "null");
			defaultInterpolatedStringHandler.AppendLiteral(", dealerSide=");
			defaultInterpolatedStringHandler.AppendFormatted((dealer != null) ? new CombatSide?(dealer.Side) : null);
			defaultInterpolatedStringHandler.AppendLiteral(", ");
			defaultInterpolatedStringHandler.AppendLiteral("charId=");
			object obj2;
			if (dealer == null)
			{
				obj2 = null;
			}
			else
			{
				Player player = dealer.Player;
				if (player == null)
				{
					obj2 = null;
				}
				else
				{
					CharacterModel character = player.Character;
					if (character == null)
					{
						obj2 = null;
					}
					else
					{
						ModelId id2 = ((AbstractModel)character).Id;
						obj2 = ((id2 != null) ? id2.Entry : null);
					}
				}
			}
			if (obj2 == null)
			{
				obj2 = "null";
			}
			defaultInterpolatedStringHandler.AppendFormatted((string?)obj2);
			defaultInterpolatedStringHandler.AppendLiteral(", ");
			defaultInterpolatedStringHandler.AppendLiteral("target=");
			defaultInterpolatedStringHandler.AppendFormatted(((target != null) ? target.Name : null) ?? "null");
			defaultInterpolatedStringHandler.AppendLiteral(", targetSide=");
			defaultInterpolatedStringHandler.AppendFormatted((target != null) ? new CombatSide?(target.Side) : null);
			GD.Print(defaultInterpolatedStringHandler.ToStringAndClear());
			if (dealer == null || (int)dealer.Side != 1)
			{
				GD.Print("[MurderDbg] 中断: dealer.Side");
				return;
			}
			if (target == null || (int)target.Side != 2)
			{
				GD.Print("[MurderDbg] 中断: target.Side");
				return;
			}
			if (cardSource == null)
			{
				GD.Print("[MurderDbg] 中断: cardSource==null");
				return;
			}
			if (!((AbstractModel)cardSource).Id.Entry.Equals("MURDER", StringComparison.OrdinalIgnoreCase))
			{
				GD.Print("[MurderDbg] 中断: cardSource不是MURDER");
				return;
			}
			Player player2 = dealer.Player;
			object obj3;
			if (player2 == null)
			{
				obj3 = null;
			}
			else
			{
				CharacterModel character2 = player2.Character;
				if (character2 == null)
				{
					obj3 = null;
				}
				else
				{
					ModelId id3 = ((AbstractModel)character2).Id;
					obj3 = ((id3 != null) ? id3.Entry : null);
				}
			}
			if ((string?)obj3 != "SILENT")
			{
				GD.Print("[MurderDbg] 中断: charId不是SILENT");
				return;
			}
			GD.Print("[MurderDbg] ★ 全部判定通过，开始染色");
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance == null)
			{
				return;
			}
			NCreature creatureNode = instance.GetCreatureNode(target);
			if (creatureNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
			{
				return;
			}
			染色.Apply(creatureNode, Colors.Red);
			Vector2 vfxSpawnPosition = creatureNode.VfxSpawnPosition;
			if (_murderVfxScene == null)
			{
				_murderVfxScene = GD.Load<PackedScene>("res://scenes/谋杀02.tscn");
				if (_murderVfxScene == null)
				{
					GD.PrintErr("[MurderSettlement] 无法加载谋杀特效场景");
					return;
				}
			}
			Node2D vfx = _murderVfxScene.Instantiate<Node2D>((GenEditState)0);
			if (vfx == null)
			{
				GD.PrintErr("[MurderSettlement] 实例化谋杀特效失败");
				return;
			}
			vfx.Scale = new Vector2(2f, 2f);
			vfx.GlobalPosition = vfxSpawnPosition;
			((Node)instance.CombatVfxContainer).AddChild((Node)(object)vfx, false, (InternalMode)0);
			float num = 0.8f;
			Timer val = new Timer();
			val.WaitTime = num;
			val.OneShot = true;
			val.Timeout += delegate
			{
				if (GodotObject.IsInstanceValid((GodotObject)(object)vfx))
				{
					((Node)vfx).QueueFree();
				}
			};
			((Node)vfx).AddChild((Node)(object)val, false, (InternalMode)0);
			val.Start(-1.0);
			RadialBlurTrigger.Trigger(0.1f, 0.3f, "MurderSettlement");
			DelayedHitStopAsync(0.1f, 0.2f);
		}
	}

	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	private static class CombatResetPatch
	{
		public static void Postfix()
		{
			染色.ClearAll();
			RadialBlurTrigger.Stop("MurderSettlement");
		}
	}

	private const string MURDER_VFX_PATH = "res://scenes/谋杀02.tscn";

	private static PackedScene _murderVfxScene;

	private const float BLUR_STRENGTH = 0.1f;

	private const float BLUR_DURATION = 0.3f;

	private const string BLUR_OWNER = "MurderSettlement";

	private static async Task DelayedHitStopAsync(float delaySec, float hitStopSec)
	{
		MainLoop mainLoop = Engine.GetMainLoop();
		SceneTree tree = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
		if (tree == null)
		{
			await Task.Delay((int)(delaySec * 1000f));
		}
		else
		{
			SceneTreeTimer timer = tree.CreateTimer((double)delaySec, true, false, false);
			await ((GodotObject)tree).ToSignal((GodotObject)(object)timer, SignalName.Timeout);
		}
		await HitStop.Apply(hitStopSec);
	}
}
