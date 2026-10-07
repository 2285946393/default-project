using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using MegaCrit.Sts2.Core.TestSupport;

namespace RegentFX.Scripts;

public static class VFXUtil
{
	public static readonly HashSet<ulong> StarryImpactNodes = new HashSet<ulong>();

	public static bool RandRD(float percentage)
	{
		return GD.Randf() < percentage;
	}

	public static Vector2 RandVec2(float beta)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2((float)GD.RandRange(-1.0, 1.0) * beta, (float)GD.RandRange(-1.0, 1.0) * beta);
	}

	public static void FitVFX(this Node2D node, Vector2 nodeStartPos, Vector2 nodeEndPos, Vector2 sceneStartPos, Vector2 sceneEndPos)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = nodeStartPos - nodeEndPos;
		Vector2 val2 = sceneStartPos - sceneEndPos;
		float rotation = ((Vector2)(ref val2)).Angle() - ((Vector2)(ref val)).Angle();
		float num = ((Vector2)(ref val2)).Length() / ((Vector2)(ref val)).Length();
		node.Rotation = rotation;
		node.Scale = Vector2.One * num;
	}

	public static Node2D? PlaySimple(string scenePath, Vector2 position, float lifetime = 2f)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		if (!TestMode.IsOn && NCombatRoom.Instance != null)
		{
			Node2D node2D = GenVFXNode(scenePath);
			GodotTreeExtensions.AddChildSafely((Node)(object)NCombatRoom.Instance.CombatVfxContainer, (Node)(object)node2D);
			node2D.GlobalPosition = position;
			((Node)node2D).GetTree().CreateTimer((double)lifetime, true, false, false).Timeout += delegate
			{
				if (GodotObject.IsInstanceValid((GodotObject)(object)node2D))
				{
					GodotTreeExtensions.QueueFreeSafely((Node)(object)node2D);
				}
			};
			return node2D;
		}
		return null;
	}

	public static Node2D? PlaySimpleBack(string scenePath, Vector2 position, float lifetime = 2f)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		if (!TestMode.IsOn && NCombatRoom.Instance != null)
		{
			Node2D node2D = GenVFXNode(scenePath);
			GodotTreeExtensions.AddChildSafely((Node)(object)NCombatRoom.Instance.BackCombatVfxContainer, (Node)(object)node2D);
			node2D.GlobalPosition = position;
			((Node)node2D).GetTree().CreateTimer((double)lifetime, true, false, false).Timeout += delegate
			{
				if (GodotObject.IsInstanceValid((GodotObject)(object)node2D))
				{
					GodotTreeExtensions.QueueFreeSafely((Node)(object)node2D);
				}
			};
			return node2D;
		}
		return null;
	}

	public static async void ShakeAfter(float time, ShakeStrength strength, ShakeDuration duration, float degAngle = -1f)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		await Wait(time);
		NGame instance = NGame.Instance;
		if (instance != null)
		{
			instance.ScreenShake(strength, duration, degAngle);
		}
	}

	public static Task Wait(float seconds, bool ignoreCombatEnd = false)
	{
		return Wait(seconds, default(CancellationToken), ignoreCombatEnd);
	}

	public static async Task Wait(float seconds, CancellationToken cancelToken, bool ignoreCombatEnd = false)
	{
		if (!NonInteractiveMode.IsActive && !((double)seconds <= 0.0) && (NGame.Instance == null || ((int)SaveManager.Instance.PrefsSave.FastMode != 3 && (ignoreCombatEnd || !CombatManager.Instance.IsEnding))))
		{
			await WaitInternal(((SceneTree)Engine.GetMainLoop()).CreateTimer((double)seconds, true, false, false), cancelToken);
		}
	}

	public static Task WaitInternal(SceneTreeTimer timer, CancellationToken cancellationToken)
	{
		TaskCompletionSource tcs = new TaskCompletionSource();
		timer.Timeout += delegate
		{
			tcs.TrySetResult();
		};
		if (cancellationToken.CanBeCanceled)
		{
			cancellationToken.Register(delegate
			{
				tcs.TrySetCanceled(cancellationToken);
			});
		}
		return tcs.Task;
	}

	public static async Task CustomScaledWait(float fastSeconds, float standardSeconds, bool ignoreCombatEnd = false, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!NonInteractiveMode.IsActive && (int)SaveManager.Instance.PrefsSave.FastMode != 3 && (ignoreCombatEnd || !CombatManager.Instance.IsEnding))
		{
			FastModeType fastMode = SaveManager.Instance.PrefsSave.FastMode;
			switch (fastMode - 1)
			{
			case 0:
				await Wait(standardSeconds, cancellationToken, ignoreCombatEnd);
				break;
			case 1:
				await Wait(fastSeconds, cancellationToken, ignoreCombatEnd);
				break;
			default:
				throw new ArgumentOutOfRangeException();
			case 2:
				break;
			}
		}
	}

	public static void PlaySpecialStarAt(Vector2 position)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		if (!TestMode.IsOn && NCombatRoom.Instance != null)
		{
			NStarryImpactVfx val = VFXUtil.GenVFXNode<NStarryImpactVfx>("res://scenes/vfx/vfx_starry_impact.tscn");
			StarryImpactNodes.Add(((GodotObject)val).GetInstanceId());
			GodotTreeExtensions.AddChildSafely((Node)(object)NCombatRoom.Instance.CombatVfxContainer, (Node)(object)val);
			((Node2D)val).GlobalPosition = position;
		}
	}

	public static T? PlaySimple<T>(string scenePath, Vector2 position) where T : Node2D
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if (!TestMode.IsOn && NCombatRoom.Instance != null)
		{
			T val = GenVFXNode<T>(scenePath);
			GodotTreeExtensions.AddChildSafely((Node)(object)NCombatRoom.Instance.CombatVfxContainer, (Node)(object)val);
			((Node2D)val).GlobalPosition = position;
			return val;
		}
		return default(T);
	}

	public static Node2D GenVFXNode(string scenePath)
	{
		if (Entry.ModSceneCache.TryGetValue(scenePath, out PackedScene value))
		{
			return value.Instantiate<Node2D>((GenEditState)0);
		}
		return PreloadManager.Cache.GetScene(scenePath).Instantiate<Node2D>((GenEditState)0);
	}

	public static T GenVFXNode<T>(string scenePath) where T : Node2D
	{
		if (Entry.ModSceneCache.TryGetValue(scenePath, out PackedScene value))
		{
			return value.Instantiate<T>((GenEditState)0);
		}
		return PreloadManager.Cache.GetScene(scenePath).Instantiate<T>((GenEditState)0);
	}

	public static void ActivateScaleAllParticles(Node2D? node, float scale)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return;
		}
		GpuParticles2D val = (GpuParticles2D)(object)((node is GpuParticles2D) ? node : null);
		if (val != null)
		{
			val.LocalCoords = true;
			((Node2D)val).Scale = ((Node2D)val).Scale * scale;
		}
		foreach (Node child in ((Node)node).GetChildren(false))
		{
			Node2D val2 = (Node2D)(object)((child is Node2D) ? child : null);
			if (val2 != null)
			{
				ActivateScaleAllParticles(val2, scale);
			}
		}
	}

	public static void ReplayAllParticles(Node2D? node)
	{
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return;
		}
		GpuParticles2D val = (GpuParticles2D)(object)((node is GpuParticles2D) ? node : null);
		if (val != null)
		{
			val.Restart();
		}
		foreach (Node child in ((Node)node).GetChildren(false))
		{
			Node2D val2 = (Node2D)(object)((child is Node2D) ? child : null);
			if (val2 != null)
			{
				ReplayAllParticles(val2);
			}
		}
	}

	public static IReadOnlyList<Creature>? GetHittableEnemiesFromCard(CardModel card)
	{
		object value = Traverse.Create((object)card).Property("CombatState", (object[])null).GetValue();
		if (value == null)
		{
			return null;
		}
		if (Traverse.Create(value).Property("HittableEnemies", (object[])null).GetValue() is IReadOnlyList<Creature> result)
		{
			return result;
		}
		return null;
	}

	public static bool IsCharacterFacingRight(Creature creature)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		object obj;
		if (instance == null)
		{
			obj = null;
		}
		else
		{
			NCreature creatureNode = instance.GetCreatureNode(creature);
			obj = ((creatureNode != null) ? creatureNode.Body : null);
		}
		Node2D val = (Node2D)obj;
		if (val == null)
		{
			return true;
		}
		return val.Scale.X > 0f;
	}

	public static Vector2? GetCombatSidePos(CardModel card)
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		object value = Traverse.Create((object)card).Property("CombatState", (object[])null).GetValue();
		if (value == null)
		{
			return null;
		}
		Traverse val = Traverse.Create(typeof(VfxCmd)).Method("GetSideCenter", new object[2]
		{
			(object)(CombatSide)2,
			value
		});
		if (val == null)
		{
			return null;
		}
		object value2 = val.GetValue();
		if (value2 is Vector2)
		{
			return (Vector2)value2;
		}
		return null;
	}

	public static Node2D? PlayOriginalVfx(Vector2 position, string path)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		if (!TestMode.IsOn && NCombatRoom.Instance != null)
		{
			string scenePath = SceneHelper.GetScenePath(path);
			Node2D val = PreloadManager.Cache.GetScene(scenePath).Instantiate<Node2D>((GenEditState)0);
			GodotTreeExtensions.AddChildSafely((Node)(object)NCombatRoom.Instance.CombatVfxContainer, (Node)(object)val);
			val.GlobalPosition = position;
			return val;
		}
		return null;
	}
}
