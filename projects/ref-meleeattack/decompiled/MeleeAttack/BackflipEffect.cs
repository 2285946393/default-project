using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class BackflipEffect
{
	[HarmonyPatch(typeof(CardModel), "OnEnqueuePlayVfx")]
	private static class CardOnPlayPatch
	{
		private static void Prefix(CardModel __instance)
		{
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Invalid comparison between Unknown and I4
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
			string text = (string)obj;
			if (!string.IsNullOrEmpty(text) && text.Equals("BACKFLIP", StringComparison.OrdinalIgnoreCase) && ShouldTrigger(__instance))
			{
				Player owner = __instance.Owner;
				Creature val = ((owner != null) ? owner.Creature : null);
				if (val != null && (int)val.Side == 1 && SimpleTeleportPatch.TryGetCreatureNode(val, out var _, out var node) && node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
				{
					PlayBackflipAsync(node);
				}
			}
		}
	}

	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	private static class NCombatRoomReadyPatch
	{
		private static void Postfix()
		{
			ClearOrigins();
		}
	}

	private const float BACKFLIP_DURATION = 0.4f;

	private const float JUMP_HEIGHT = 300f;

	private const float WAIST_OFFSET = 150f;

	private const float ROTATION_ANGLE = (float)Math.PI * -2f;

	private const float HOVER_DURATION = 0.3f;

	private const float HOVER_DROP = 300f;

	private static readonly ConcurrentDictionary<NCreature, Task> _activeBackflips = new ConcurrentDictionary<NCreature, Task>();

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

	private static async Task WaitForReturnFinishedAsync(NCreature rootNode)
	{
		if (rootNode != null && GodotObject.IsInstanceValid((GodotObject)(object)rootNode))
		{
			long startTicks = (long)Time.GetTicksMsec();
			while (GodotObject.IsInstanceValid((GodotObject)(object)rootNode) && (long)Time.GetTicksMsec() - startTicks <= 5000 && SimpleTeleportPatch.IsReturningInProgress(rootNode))
			{
				await Task.Delay(16);
			}
		}
	}

	public static async Task PlayBackflipAsync(NCreature rootNode)
	{
		if (!SettingsUI.IsBackflipEnabled() || rootNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)rootNode))
		{
			return;
		}
		await MurderEffect.WaitIfUnstoppableAsync(rootNode);
		if (!GodotObject.IsInstanceValid((GodotObject)(object)rootNode))
		{
			return;
		}
		await WaitForReturnFinishedAsync(rootNode);
		if (!GodotObject.IsInstanceValid((GodotObject)(object)rootNode))
		{
			return;
		}
		if (_activeBackflips.TryGetValue(rootNode, out var prevTask) && !prevTask.IsCompleted)
		{
			try
			{
				await prevTask;
			}
			catch
			{
			}
		}
		if (!GodotObject.IsInstanceValid((GodotObject)(object)rootNode))
		{
			return;
		}
		TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
		_activeBackflips[rootNode] = tcs.Task;
		try
		{
			await PlayBackflipInternalAsync(rootNode);
		}
		finally
		{
			tcs.TrySetResult(result: true);
			if (_activeBackflips.TryGetValue(rootNode, out var stored) && stored == tcs.Task)
			{
				_activeBackflips.TryRemove(rootNode, out var _);
			}
		}
	}

	private static async Task PlayBackflipInternalAsync(NCreature rootNode)
	{
		if (rootNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)rootNode))
		{
			return;
		}
		SimpleTeleportPatch.AttackSequenceState state = SimpleTeleportPatch.GetOrCreateAttackState(homePos: ((Control)rootNode).GlobalPosition, node: rootNode);
		Vector2 lockedHomePos = state.LockedHomePos;
		state.SkipFadeAndStretchOnReturn = true;
		if (!state.IsReturning && SimpleTeleportPatch._returnCtsTable.TryRemove(rootNode, out var oldCts))
		{
			oldCts?.Cancel();
			oldCts?.Dispose();
		}
		if (!SimpleTeleportPatch._initialScales.ContainsKey(rootNode))
		{
			SimpleTeleportPatch._initialScales[rootNode] = new SimpleTeleportPatch.Vector2Wrapper(((Control)rootNode).Scale);
		}
		Node2D visual = rootNode.Body;
		if (visual != null && GodotObject.IsInstanceValid((GodotObject)(object)visual) && !state.VisualOriginalPosition.HasValue)
		{
			state.VisualOriginalPosition = visual.Position;
			state.VisualOriginalRotation = visual.Rotation;
			state.IsBackflipState = true;
		}
		CardSmogEffect.HideMirrorEffect(rootNode);
		Xue.MakeHealthBarTransparent(rootNode);
		if (!SimpleTeleportPatch._originalZIndices.ContainsKey(rootNode))
		{
			SimpleTeleportPatch._originalZIndices[rootNode] = new SimpleTeleportPatch.IntWrapper(((CanvasItem)rootNode).ZIndex);
		}
		((CanvasItem)rootNode).ZIndex = 5;
		Vector2 startPos = ((Control)rootNode).GlobalPosition;
		float duration = 0.4f;
		float maxHeight = 300f;
		float waistOffset = 150f;
		if (visual == null)
		{
			CardSmogEffect.ShowMirrorEffect(rootNode);
			return;
		}
		try
		{
			Vector2 baseVisualPos = (Vector2)(((_003F?)state.VisualOriginalPosition) ?? visual.Position);
			float baseVisualRot = state.VisualOriginalRotation ?? visual.Rotation;
			Tween tween = ((Node)rootNode).CreateTween();
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
			tween.SetParallel(true);
			tween.TweenMethod(Callable.From<float>((Action<float>)delegate(float t)
			{
				//IL_0058: Unknown result type (might be due to invalid IL or missing references)
				float num3 = Mathf.Clamp(t / duration, 0f, 1f);
				float num4 = 1f - Mathf.Pow(1f - num3, 2f);
				float num5 = maxHeight * num4;
				((Control)rootNode).GlobalPosition = new Vector2(startPos.X, startPos.Y - num5);
			}), Variant.op_Implicit(0f), Variant.op_Implicit(duration), (double)duration).SetTrans((TransitionType)4).SetEase((EaseType)1);
			tween.TweenMethod(Callable.From<float>((Action<float>)delegate(float t)
			{
				//IL_0069: Unknown result type (might be due to invalid IL or missing references)
				//IL_006e: Unknown result type (might be due to invalid IL or missing references)
				//IL_007b: Unknown result type (might be due to invalid IL or missing references)
				//IL_007c: Unknown result type (might be due to invalid IL or missing references)
				//IL_007e: Unknown result type (might be due to invalid IL or missing references)
				float num = Mathf.Clamp(t / duration, 0f, 1f);
				float num2 = (float)Math.PI * -2f * num;
				Vector2 val = default(Vector2);
				((Vector2)(ref val))._002Ector(baseVisualPos.X, baseVisualPos.Y - waistOffset);
				Vector2 val2 = default(Vector2);
				((Vector2)(ref val2))._002Ector(0f, waistOffset);
				Vector2 val3 = ((Vector2)(ref val2)).Rotated(num2);
				visual.Position = val + val3;
				visual.Rotation = baseVisualRot + num2;
			}), Variant.op_Implicit(0f), Variant.op_Implicit(duration), (double)duration).SetTrans((TransitionType)4).SetEase((EaseType)2);
			tween.Finished += delegate
			{
				tcs.SetResult(result: true);
			};
			await tcs.Task;
			Vector2 dropTargetPos = new Vector2(startPos.X, startPos.Y - maxHeight + 300f);
			CancellationTokenSource hoverCts = new CancellationTokenSource();
			SimpleTeleportPatch._returnCtsTable[rootNode] = hoverCts;
			Tween hoverTween = ((Node)rootNode).CreateTween();
			TaskCompletionSource<bool> hoverTcs = new TaskCompletionSource<bool>();
			hoverTween.TweenProperty((GodotObject)(object)rootNode, NodePath.op_Implicit("global_position:y"), Variant.op_Implicit(dropTargetPos.Y), 0.30000001192092896).SetTrans((TransitionType)4).SetEase((EaseType)0);
			hoverTween.Finished += delegate
			{
				hoverTcs.SetResult(result: true);
			};
			try
			{
				using (hoverCts.Token.Register(delegate
				{
					if (GodotObject.IsInstanceValid((GodotObject)(object)hoverTween))
					{
						hoverTween.Kill();
					}
					hoverTcs.TrySetCanceled();
				}))
				{
					await hoverTcs.Task;
				}
			}
			catch (OperationCanceledException)
			{
				return;
			}
			string charId = SimpleTeleportPatch.GetCreatureId(rootNode.Entity);
			float extraBuffer = SettingsUI.GetDelayForCharacter(charId);
			CancellationTokenSource returnCts = new CancellationTokenSource();
			SimpleTeleportPatch._returnCtsTable[rootNode] = returnCts;
			await SimpleTeleportPatch.ReturnAfterDelay(rootNode, lockedHomePos, extraBuffer, returnCts.Token, visual);
		}
		finally
		{
			if (rootNode != null && GodotObject.IsInstanceValid((GodotObject)(object)rootNode))
			{
				CardSmogEffect.ShowMirrorEffect(rootNode);
			}
		}
	}

	public static void ClearOrigins()
	{
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return;
		}
		foreach (NCreature creatureNode in instance.CreatureNodes)
		{
			if (creatureNode != null && ((GodotObject)creatureNode).HasMeta(StringName.op_Implicit("__backflip_origin")))
			{
				((GodotObject)creatureNode).RemoveMeta(StringName.op_Implicit("__backflip_origin"));
			}
		}
	}
}
