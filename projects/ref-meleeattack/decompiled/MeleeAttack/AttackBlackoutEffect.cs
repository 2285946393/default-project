using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class AttackBlackoutEffect
{
	private class BlackoutInstance
	{
		public int Id;

		public ColorRect BlackRect;

		public List<NCreature> AffectedNodes;

		public TaskCompletionSource<bool> HoldTcs;

		public bool IsCleanedUp;

		public bool PendingEnd;
	}

	private static BlackoutInstance _active;

	private static int _instanceCounter = 0;

	private static readonly object _lock = new object();

	private const float BLACKOUT_SIZE_MULTIPLIER = 1.3f;

	private static int _playContainerHideCount = 0;

	private static Control _globalPlayContainer;

	private static Control _globalPlayQueue;

	private static bool _globalPlayContainerWasVisible;

	private static bool _globalPlayQueueWasVisible;

	public static bool IsWaiting
	{
		get
		{
			lock (_lock)
			{
				return _active != null && _active.HoldTcs != null && !_active.HoldTcs.Task.IsCompleted;
			}
		}
	}

	public static async Task TriggerBlackout(float fadeInDuration = 0.2f, float holdDuration = 0.5f, float fadeOutDuration = 0.3f)
	{
		NCombatRoom room = NCombatRoom.Instance;
		if (room == null)
		{
			return;
		}
		BlackoutInstance instance;
		BlackoutInstance prev;
		lock (_lock)
		{
			_instanceCounter++;
			instance = new BlackoutInstance
			{
				Id = _instanceCounter
			};
			prev = _active;
			_active = instance;
		}
		prev?.HoldTcs?.TrySetResult(result: true);
		Node container = (Node)(((object)room.BackCombatVfxContainer) ?? ((object)room));
		CanvasItem canvasItem = (CanvasItem)(((object)((container is CanvasItem) ? container : null)) ?? ((object)room));
		Rect2 viewportRect = ((Node)room).GetViewport().GetVisibleRect();
		Transform2D containerTransform = canvasItem.GetGlobalTransform();
		Transform2D inverse = ((Transform2D)(ref containerTransform)).AffineInverse();
		Vector2 localTopLeft = inverse * ((Rect2)(ref viewportRect)).Position;
		Vector2 localBottomRight = inverse * (((Rect2)(ref viewportRect)).Position + ((Rect2)(ref viewportRect)).Size);
		Vector2 localCenter = (localTopLeft + localBottomRight) * 0.5f;
		Vector2 localSize = (localBottomRight - localTopLeft) * 1.3f;
		Vector2 localPos = localCenter - localSize * 0.5f;
		instance.AffectedNodes = new List<NCreature>();
		foreach (NCreature creatureNode in room.CreatureNodes)
		{
			if (((creatureNode != null) ? creatureNode.Entity : null) != null && creatureNode.Entity.IsAlive)
			{
				instance.AffectedNodes.Add(creatureNode);
			}
		}
		foreach (NCreature node in instance.AffectedNodes)
		{
			Control hpBar = Xue.FindHealthBar(node);
			if (hpBar != null)
			{
				Color c2 = ((CanvasItem)hpBar).Modulate;
				c2.A = 0f;
				((CanvasItem)hpBar).Modulate = c2;
			}
			List<Control> statusContainers = Xue.FindStatusContainers(node);
			foreach (Control status in statusContainers)
			{
				Color c = ((CanvasItem)status).Modulate;
				c.A = 0f;
				((CanvasItem)status).Modulate = c;
			}
		}
		HidePlayContainers(room);
		instance.BlackRect = new ColorRect
		{
			Position = localPos,
			Size = localSize,
			Color = new Color(0f, 0f, 0f, 0.75f),
			Modulate = new Color(1f, 1f, 1f, 0f),
			MouseFilter = (MouseFilterEnum)2
		};
		container.AddChild((Node)(object)instance.BlackRect, false, (InternalMode)0);
		try
		{
			Tween tween = ((Node)room).CreateTween();
			tween.TweenProperty((GodotObject)(object)instance.BlackRect, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(1f), (double)fadeInDuration).SetEase((EaseType)2);
			await ((GodotObject)room).ToSignal((GodotObject)(object)tween, SignalName.Finished);
			lock (_lock)
			{
				if (_active != instance)
				{
					return;
				}
				instance.HoldTcs = new TaskCompletionSource<bool>();
				if (instance.PendingEnd)
				{
					instance.PendingEnd = false;
					Task.Run(() => EndBlackout(fadeOutDuration));
				}
				goto IL_0679;
			}
			IL_0679:
			await instance.HoldTcs.Task;
		}
		finally
		{
			await CleanupInstance(instance, fadeOutDuration);
			lock (_lock)
			{
				if (_active == instance)
				{
					_active = null;
				}
			}
		}
	}

	public static Task EndBlackout(float fadeOutDuration = 0.3f)
	{
		BlackoutInstance active;
		lock (_lock)
		{
			active = _active;
			if (active == null || active.IsCleanedUp)
			{
				return Task.CompletedTask;
			}
			if (active.HoldTcs == null || active.HoldTcs.Task.IsCompleted)
			{
				active.PendingEnd = true;
				return Task.CompletedTask;
			}
		}
		active.HoldTcs.TrySetResult(result: true);
		return Task.CompletedTask;
	}

	private static async Task CleanupInstance(BlackoutInstance instance, float fadeOutDuration)
	{
		if (!(instance?.IsCleanedUp ?? true))
		{
			instance.IsCleanedUp = true;
			if (instance.BlackRect != null && GodotObject.IsInstanceValid((GodotObject)(object)instance.BlackRect))
			{
				Tween tween = ((Node)instance.BlackRect).CreateTween();
				tween.TweenProperty((GodotObject)(object)instance.BlackRect, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), (double)fadeOutDuration).SetEase((EaseType)2);
				await ((GodotObject)instance.BlackRect).ToSignal((GodotObject)(object)tween, SignalName.Finished);
			}
			RestoreCreatureOverlays(instance);
			ShowPlayContainers();
			if (instance.BlackRect != null && GodotObject.IsInstanceValid((GodotObject)(object)instance.BlackRect))
			{
				((Node)instance.BlackRect).QueueFree();
			}
			instance.BlackRect = null;
		}
	}

	private static void RestoreCreatureOverlays(BlackoutInstance instance)
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Invalid comparison between Unknown and I4
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		if (instance.AffectedNodes == null)
		{
			return;
		}
		foreach (NCreature affectedNode in instance.AffectedNodes)
		{
			if (affectedNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)affectedNode))
			{
				continue;
			}
			Creature entity = affectedNode.Entity;
			if (entity == null || !entity.IsAlive || (int)affectedNode.Entity.Side == 1)
			{
				continue;
			}
			Control val = Xue.FindHealthBar(affectedNode);
			if (val != null)
			{
				Color modulate = ((CanvasItem)val).Modulate;
				modulate.A = 1f;
				((CanvasItem)val).Modulate = modulate;
			}
			List<Control> list = Xue.FindStatusContainers(affectedNode);
			foreach (Control item in list)
			{
				if (GodotObject.IsInstanceValid((GodotObject)(object)item))
				{
					Color modulate2 = ((CanvasItem)item).Modulate;
					modulate2.A = 1f;
					((CanvasItem)item).Modulate = modulate2;
				}
			}
		}
		instance.AffectedNodes.Clear();
		instance.AffectedNodes = null;
	}

	private static void HidePlayContainers(NCombatRoom room)
	{
		NCombatUi val = ((room != null) ? room.Ui : null);
		if (val == null)
		{
			return;
		}
		lock (_lock)
		{
			if (_playContainerHideCount == 0)
			{
				_globalPlayContainer = val.PlayContainer;
				_globalPlayQueue = (Control)(object)val.PlayQueue;
				if (_globalPlayContainer != null)
				{
					_globalPlayContainerWasVisible = ((CanvasItem)_globalPlayContainer).Visible;
					((CanvasItem)_globalPlayContainer).Visible = false;
				}
				if (_globalPlayQueue != null)
				{
					_globalPlayQueueWasVisible = ((CanvasItem)_globalPlayQueue).Visible;
					((CanvasItem)_globalPlayQueue).Visible = false;
				}
			}
			_playContainerHideCount++;
		}
	}

	private static void ShowPlayContainers()
	{
		lock (_lock)
		{
			if (_playContainerHideCount > 0)
			{
				_playContainerHideCount--;
			}
			if (_playContainerHideCount == 0)
			{
				if (_globalPlayContainer != null && GodotObject.IsInstanceValid((GodotObject)(object)_globalPlayContainer))
				{
					((CanvasItem)_globalPlayContainer).Visible = _globalPlayContainerWasVisible;
				}
				if (_globalPlayQueue != null && GodotObject.IsInstanceValid((GodotObject)(object)_globalPlayQueue))
				{
					((CanvasItem)_globalPlayQueue).Visible = _globalPlayQueueWasVisible;
				}
				_globalPlayContainer = null;
				_globalPlayQueue = null;
			}
		}
	}
}
