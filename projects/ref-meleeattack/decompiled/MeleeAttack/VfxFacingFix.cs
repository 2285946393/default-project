using System;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class VfxFacingFix
{
	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	private static class NCombatRoomReadyPatch
	{
		[CompilerGenerated]
		private static class _003C_003EO
		{
			public static NodeAddedEventHandler _003C0_003E__OnNodeAdded;
		}

		private static void Postfix(NCombatRoom __instance)
		{
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Expected O, but got Unknown
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Expected O, but got Unknown
			if (__instance == null)
			{
				return;
			}
			SceneTree tree = ((Node)__instance).GetTree();
			if (tree == null)
			{
				return;
			}
			if (_subscribed)
			{
				object obj = _003C_003EO._003C0_003E__OnNodeAdded;
				if (obj == null)
				{
					NodeAddedEventHandler val = OnNodeAdded;
					_003C_003EO._003C0_003E__OnNodeAdded = val;
					obj = (object)val;
				}
				tree.NodeAdded -= (NodeAddedEventHandler)obj;
			}
			object obj2 = _003C_003EO._003C0_003E__OnNodeAdded;
			if (obj2 == null)
			{
				NodeAddedEventHandler val2 = OnNodeAdded;
				_003C_003EO._003C0_003E__OnNodeAdded = val2;
				obj2 = (object)val2;
			}
			tree.NodeAdded += (NodeAddedEventHandler)obj2;
			_subscribed = true;
		}
	}

	private const string VFX_CONTAINER_NAME_HINT = "VfxContainer";

	private static readonly string[] _whitelist = new string[2] { "fx_dagger_spray_flurry", "vfx_sweeping_beam" };

	private static NCreature _activeAttacker;

	private static bool _subscribed = false;

	public static void SetActiveAttacker(NCreature node)
	{
		_activeAttacker = node;
	}

	public static void ClearActiveAttacker()
	{
		_activeAttacker = null;
	}

	private static NCreature ResolveAttacker()
	{
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Invalid comparison between Unknown and I4
		if (_activeAttacker != null && GodotObject.IsInstanceValid((GodotObject)(object)_activeAttacker))
		{
			return _activeAttacker;
		}
		try
		{
			AttackCommand value = SimpleTeleportPatch._currentAttack.Value;
			if (value != null)
			{
				AbstractModel modelSource = value.ModelSource;
				CardModel val = (CardModel)(object)((modelSource is CardModel) ? modelSource : null);
				object obj;
				if (val == null)
				{
					obj = null;
				}
				else
				{
					Player owner = val.Owner;
					obj = ((owner != null) ? owner.Creature : null);
				}
				Creature val2 = (Creature)obj;
				if (val2 != null && SimpleTeleportPatch.TryGetCreatureNode(val2, out var _, out var node))
				{
					return node;
				}
			}
		}
		catch
		{
		}
		try
		{
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance != null)
			{
				foreach (NCreature creatureNode in instance.CreatureNodes)
				{
					if (creatureNode != null)
					{
						Creature entity = creatureNode.Entity;
						if ((int)((entity != null) ? new CombatSide?(entity.Side) : null).GetValueOrDefault() == 1 && creatureNode.Entity.IsAlive)
						{
							return creatureNode;
						}
					}
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static bool IsWhitelisted(Node2D node)
	{
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return false;
		}
		string text = ((Node)node).SceneFilePath ?? "";
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		string[] whitelist = _whitelist;
		foreach (string value in whitelist)
		{
			if (text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsDirectChildOfVfxContainer(Node2D node)
	{
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return false;
		}
		Node parent = ((Node)node).GetParent();
		if (parent == null)
		{
			return false;
		}
		string text = ((object)parent.Name).ToString();
		if (!string.IsNullOrEmpty(text) && text.IndexOf("VfxContainer", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		if (parent is NCombatRoom)
		{
			return true;
		}
		return false;
	}

	private static void OnNodeAdded(Node node)
	{
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		Node2D n2d = (Node2D)(object)((node is Node2D) ? node : null);
		if (n2d != null && GodotObject.IsInstanceValid((GodotObject)(object)n2d) && IsWhitelisted(n2d) && IsDirectChildOfVfxContainer(n2d))
		{
			Callable val = Callable.From((Action)delegate
			{
				ApplyFacingFix(n2d);
			});
			((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
		}
	}

	private static void ApplyFacingFix(Node2D outerNode)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		if (outerNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)outerNode))
		{
			return;
		}
		NCreature val = ResolveAttacker();
		if (val == null || !GodotObject.IsInstanceValid((GodotObject)(object)val))
		{
			return;
		}
		Node2D visualNode = SimpleTeleportPatch.GetVisualNode(val);
		if (visualNode != null && GodotObject.IsInstanceValid((GodotObject)(object)visualNode))
		{
			float x = visualNode.Scale.X;
			float num = Math.Sign(x);
			if (Math.Abs(x) < 0.001f)
			{
				num = 1f;
			}
			if (!(num >= 0f))
			{
				PrepareParticlesForFlipping((Node)(object)outerNode);
				outerNode.Scale = new Vector2(0f - outerNode.Scale.X, outerNode.Scale.Y);
				RestartAllParticles((Node)(object)outerNode);
			}
		}
	}

	private static void PrepareParticlesForFlipping(Node parent)
	{
		foreach (Node child in parent.GetChildren(false))
		{
			GpuParticles2D val = (GpuParticles2D)(object)((child is GpuParticles2D) ? child : null);
			if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val))
			{
				if (!val.LocalCoords)
				{
					val.LocalCoords = true;
				}
			}
			else
			{
				CpuParticles2D val2 = (CpuParticles2D)(object)((child is CpuParticles2D) ? child : null);
				if (val2 != null && GodotObject.IsInstanceValid((GodotObject)(object)val2) && !val2.LocalCoords)
				{
					val2.LocalCoords = true;
				}
			}
			PrepareParticlesForFlipping(child);
		}
	}

	private static void RestartAllParticles(Node parent)
	{
		foreach (Node child in parent.GetChildren(false))
		{
			GpuParticles2D val = (GpuParticles2D)(object)((child is GpuParticles2D) ? child : null);
			if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val))
			{
				val.Restart();
			}
			else
			{
				CpuParticles2D val2 = (CpuParticles2D)(object)((child is CpuParticles2D) ? child : null);
				if (val2 != null && GodotObject.IsInstanceValid((GodotObject)(object)val2))
				{
					val2.Restart();
				}
			}
			RestartAllParticles(child);
		}
	}
}
