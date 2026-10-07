using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class MonsterSplitViewport
{
	[HarmonyPatch(typeof(AttackCommand), "Execute")]
	private static class SliceCardTriggerPatch
	{
		private static void Prefix(AttackCommand __instance)
		{
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0075: Invalid comparison between Unknown and I4
			if (!SettingsUI.IsSliceEnabled())
			{
				return;
			}
			AbstractModel modelSource = __instance.ModelSource;
			CardModel val = (CardModel)(object)((modelSource is CardModel) ? modelSource : null);
			if (val == null)
			{
				return;
			}
			ModelId id = ((AbstractModel)val).Id;
			if (((id != null) ? id.Entry : null) == null || !((AbstractModel)val).Id.Entry.Equals("SLICE", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
			Creature targetCreature = GetTargetCreature(__instance);
			if (targetCreature == null || (int)targetCreature.Side != 2)
			{
				return;
			}
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance != null)
			{
				NCreature creatureNode = instance.GetCreatureNode(targetCreature);
				if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
				{
					StartSplit(creatureNode);
				}
			}
		}

		private static Creature GetTargetCreature(AttackCommand command)
		{
			//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b2: Expected O, but got Unknown
			try
			{
				FieldInfo field = typeof(AttackCommand).GetField("_singleTarget", BindingFlags.Instance | BindingFlags.NonPublic);
				if (field != null)
				{
					object? value = field.GetValue(command);
					Creature val = (Creature)((value is Creature) ? value : null);
					if (val != null && val.IsAlive)
					{
						return val;
					}
				}
			}
			catch
			{
			}
			try
			{
				MethodInfo method = typeof(AttackCommand).GetMethod("GetPossibleTargets", BindingFlags.Instance | BindingFlags.NonPublic);
				if (method != null && method.Invoke(command, null) is IEnumerable enumerable)
				{
					foreach (Creature item in enumerable)
					{
						Creature val2 = item;
						if (val2 != null && val2.IsAlive)
						{
							return val2;
						}
					}
				}
			}
			catch
			{
			}
			return null;
		}
	}

	private const float FIRST_CUT_ANGLE = 45f;

	private const float SECOND_CUT_ANGLE = 135f;

	private const float WOUND_THICKNESS = 2f;

	private static readonly Color WOUND_COLOR = new Color(0.639f, 0.933f, 0.749f, 1f);

	private const float LINE_DISPLAY_DURATION = 0.04f;

	private const int LINE_SEGMENTS = 5;

	private const float SPLIT_MOVE_DURATION = 0.1f;

	private const float SPLIT_DISTANCE = 5f;

	private const float SECOND_SPLIT_DISTANCE = 50f;

	private const float SECOND_SPLIT_DURATION = 0.1f;

	private const float SPLIT_LIFETIME = 0.2f;

	public static async Task StartSplit(NCreature creatureNode)
	{
		await SplitMonsterDynamic(creatureNode);
	}

	private static async Task SplitMonsterDynamic(NCreature creatureNode)
	{
		Node2D visualNode = creatureNode.Body;
		if (visualNode == null)
		{
			return;
		}
		NCombatRoom room = NCombatRoom.Instance;
		if (room == null)
		{
			return;
		}
		Node parent = (Node)(object)room;
		Rect2 visualBounds = GetVisualBounds(creatureNode);
		if (((Rect2)(ref visualBounds)).Size.X < 10f || ((Rect2)(ref visualBounds)).Size.Y < 10f)
		{
			GD.PrintErr("[SplitViewport] 无法获取有效的视觉边界");
			return;
		}
		Vector2 visualCenter = ((Rect2)(ref visualBounds)).GetCenter();
		Vector2 visualSize = ((Rect2)(ref visualBounds)).Size;
		Vector2 rootPos = visualNode.GlobalPosition;
		Vector2 offset = visualCenter - rootPos;
		Vector2 viewportSize = visualSize;
		Vector2 viewportOrigin = visualCenter - viewportSize * 0.5f;
		string currentAnim = null;
		float currentTime = 0f;
		SpineAnimationAccess spine = creatureNode.SpineAnimation;
		if (((SpineAnimationAccess)(ref spine)).IsValid)
		{
			MegaTrackEntry track = ((SpineAnimationAccess)(ref spine)).GetCurrentTrack(0);
			if (track != null)
			{
				currentAnim = track.GetAnimationName();
				currentTime = track.GetTrackTime();
			}
		}
		Node2D copy = (Node2D)((Node)visualNode).Duplicate(15);
		if (copy == null)
		{
			return;
		}
		((CanvasItem)copy).Visible = true;
		((CanvasItem)copy).Modulate = Colors.White;
		copy.Position = viewportSize * 0.5f - offset;
		if (!string.IsNullOrEmpty(currentAnim) && ((SpineAnimationAccess)(ref spine)).IsValid)
		{
			SpineNodeExtensions.RunWhenSpineReady((Node)(object)copy, new MegaSprite(Variant.op_Implicit((GodotObject)(object)copy)), (Action<MegaAnimationState>)delegate(MegaAnimationState state)
			{
				MethodInfo method = ((object)state).GetType().GetMethod("SetAnimation", new Type[3]
				{
					typeof(string),
					typeof(bool),
					typeof(int)
				});
				if (method != null)
				{
					method.Invoke(state, new object[3] { currentAnim, false, 0 });
				}
				else
				{
					((object)state).GetType().GetMethod("SetAnimation", new Type[2]
					{
						typeof(string),
						typeof(bool)
					})?.Invoke(state, new object[2] { currentAnim, false });
				}
				MegaTrackEntry current = state.GetCurrent(0);
				if (current != null)
				{
					current.SetTrackTime(currentTime);
					((MegaSpineBinding)current).Dispose();
				}
				state.SetTimeScale(0f);
			});
		}
		SubViewportContainer container = new SubViewportContainer();
		((Control)container).Position = viewportOrigin;
		((Control)container).Size = viewportSize;
		((Control)container).MouseFilter = (MouseFilterEnum)2;
		((Control)container).ClipContents = true;
		SubViewport viewport = new SubViewport();
		viewport.Size = (Vector2I)viewportSize;
		((Viewport)viewport).TransparentBg = true;
		viewport.RenderTargetUpdateMode = (UpdateMode)4;
		((Node)viewport).Name = StringName.op_Implicit("SplitViewport");
		((Node)viewport).AddChild((Node)(object)copy, false, (InternalMode)0);
		((Node)container).AddChild((Node)(object)viewport, false, (InternalMode)0);
		parent.AddChild((Node)(object)container, false, (InternalMode)0);
		await ((GodotObject)parent).ToSignal((GodotObject)(object)Engine.GetMainLoop(), StringName.op_Implicit("process_frame"));
		await ((GodotObject)parent).ToSignal((GodotObject)(object)Engine.GetMainLoop(), StringName.op_Implicit("process_frame"));
		Texture2D texture = (Texture2D)(object)((Viewport)viewport).GetTexture();
		if (texture == null)
		{
			((Node)container).QueueFree();
			return;
		}
		Image fullImage = texture.GetImage();
		if (fullImage == null)
		{
			((Node)container).QueueFree();
			return;
		}
		((CanvasItem)visualNode).Visible = false;
		((Node)container).QueueFree();
		Vector2 centerInViewport = viewportSize * 0.5f;
		float diagonal = ((Vector2)(ref visualSize)).Length();
		float woundHalfLength = Math.Max(diagonal * 0.8f, 50f);
		SplitImageDiagonal(fullImage, out var leftImage, out var rightImage, centerInViewport, 45f, showWound: false, 0f);
		TextureRect leftRect = CreateTextureRect(leftImage, viewportSize);
		TextureRect rightRect = CreateTextureRect(rightImage, viewportSize);
		((Control)leftRect).GlobalPosition = viewportOrigin;
		((Control)rightRect).GlobalPosition = viewportOrigin;
		parent.AddChild((Node)(object)leftRect, false, (InternalMode)0);
		parent.AddChild((Node)(object)rightRect, false, (InternalMode)0);
		Vector2 worldCenter = visualCenter;
		float rad1 = (float)Math.PI / 4f;
		Vector2 dir2 = new Vector2(Mathf.Cos(rad1), Mathf.Sin(rad1));
		Vector2 start1 = worldCenter - dir2 * woundHalfLength;
		Vector2 end1 = worldCenter + dir2 * woundHalfLength;
		await DrawCutLine(parent, start1, end1);
		Tween tween = parent.CreateTween();
		tween.SetParallel(true);
		float d = 5f;
		Vector2 leftTarget = ((Control)leftRect).GlobalPosition + new Vector2(0f - d, 0f - d);
		Vector2 rightTarget = ((Control)rightRect).GlobalPosition + new Vector2(d, d);
		tween.TweenProperty((GodotObject)(object)leftRect, NodePath.op_Implicit("global_position"), Variant.op_Implicit(leftTarget), 0.10000000149011612).SetTrans((TransitionType)4).SetEase((EaseType)1);
		tween.TweenProperty((GodotObject)(object)rightRect, NodePath.op_Implicit("global_position"), Variant.op_Implicit(rightTarget), 0.10000000149011612).SetTrans((TransitionType)4).SetEase((EaseType)1);
		await ((GodotObject)parent).ToSignal((GodotObject)(object)tween, SignalName.Finished);
		Image leftImg = leftRect.Texture.GetImage();
		Image rightImg = rightRect.Texture.GetImage();
		if (leftImg == null || rightImg == null)
		{
			return;
		}
		SplitImageDiagonal(leftImg, out var leftTop, out var leftBottom, centerInViewport, 135f, showWound: false, 0f);
		SplitImageDiagonal(rightImg, out var rightTop, out var rightBottom, centerInViewport, 135f, showWound: false, 0f);
		TextureRect topLeft = CreateTextureRect(leftTop, viewportSize);
		TextureRect topRight = CreateTextureRect(leftBottom, viewportSize);
		TextureRect bottomLeft = CreateTextureRect(rightTop, viewportSize);
		TextureRect bottomRight = CreateTextureRect(rightBottom, viewportSize);
		((Control)topLeft).GlobalPosition = ((Control)leftRect).GlobalPosition;
		((Control)topRight).GlobalPosition = ((Control)leftRect).GlobalPosition;
		((Control)bottomLeft).GlobalPosition = ((Control)rightRect).GlobalPosition;
		((Control)bottomRight).GlobalPosition = ((Control)rightRect).GlobalPosition;
		parent.AddChild((Node)(object)topLeft, false, (InternalMode)0);
		parent.AddChild((Node)(object)topRight, false, (InternalMode)0);
		parent.AddChild((Node)(object)bottomLeft, false, (InternalMode)0);
		parent.AddChild((Node)(object)bottomRight, false, (InternalMode)0);
		((Node)leftRect).QueueFree();
		((Node)rightRect).QueueFree();
		float rad2 = (float)Math.PI * 3f / 4f;
		Vector2 dir3 = new Vector2(Mathf.Cos(rad2), Mathf.Sin(rad2));
		Vector2 start2 = worldCenter - dir3 * woundHalfLength;
		Vector2 end2 = worldCenter + dir3 * woundHalfLength;
		await DrawCutLine(parent, start2, end2);
		List<TextureRect> pieces = new List<TextureRect> { topLeft, topRight, bottomLeft, bottomRight };
		List<(TextureRect rect, Vector2 center)> rectCenters = new List<(TextureRect, Vector2)>();
		foreach (TextureRect rect3 in pieces)
		{
			Vector2 center = ((Control)rect3).GlobalPosition + ((Control)rect3).Size * 0.5f;
			rectCenters.Add((rect3, center));
		}
		rectCenters.Sort(((TextureRect rect, Vector2 center) a, (TextureRect rect, Vector2 center) b) => a.center.Y.CompareTo(b.center.Y));
		TextureRect top = rectCenters[0].rect;
		TextureRect bottom = rectCenters[3].rect;
		List<(TextureRect rect, Vector2 center)> remaining = new List<(TextureRect, Vector2)>();
		foreach (var item in rectCenters)
		{
			if (item.rect != top && item.rect != bottom)
			{
				remaining.Add(item);
			}
		}
		remaining.Sort(((TextureRect rect, Vector2 center) a, (TextureRect rect, Vector2 center) b) => a.center.X.CompareTo(b.center.X));
		TextureRect left = remaining[0].rect;
		TextureRect right = remaining[1].rect;
		Dictionary<TextureRect, Vector2> dirMap = new Dictionary<TextureRect, Vector2>
		{
			{
				top,
				Vector2.Up
			},
			{
				bottom,
				Vector2.Down
			},
			{
				left,
				Vector2.Left
			},
			{
				right,
				Vector2.Right
			}
		};
		Tween tween2 = parent.CreateTween();
		tween2.SetParallel(true);
		foreach (TextureRect rect2 in pieces)
		{
			Vector2 dir = dirMap[rect2];
			Vector2 targetPos = ((Control)rect2).GlobalPosition + dir * 50f;
			tween2.TweenProperty((GodotObject)(object)rect2, NodePath.op_Implicit("global_position"), Variant.op_Implicit(targetPos), 0.10000000149011612).SetTrans((TransitionType)4).SetEase((EaseType)1);
		}
		await ((GodotObject)parent).ToSignal((GodotObject)(object)tween2, SignalName.Finished);
		await Task.Delay(200);
		foreach (TextureRect rect in pieces)
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)rect))
			{
				((Node)rect).QueueFree();
			}
		}
		if (GodotObject.IsInstanceValid((GodotObject)(object)visualNode))
		{
			((CanvasItem)visualNode).Visible = true;
		}
	}

	private static async Task DrawCutLine(Node parent, Vector2 start, Vector2 end)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		Line2D line = new Line2D();
		line.Width = 2f;
		line.DefaultColor = WOUND_COLOR;
		parent.AddChild((Node)(object)line, false, (InternalMode)0);
		int delayPerSegment = 8;
		for (int step = 0; step <= 5; step++)
		{
			float t = (float)step / 5f;
			Vector2 currentPoint = ((Vector2)(ref start)).Lerp(end, t);
			line.AddPoint(currentPoint, -1);
			await Task.Delay(delayPerSegment);
		}
		if (GodotObject.IsInstanceValid((GodotObject)(object)line))
		{
			((Node)line).QueueFree();
		}
	}

	private static Rect2 GetVisualBounds(NCreature creatureNode)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		if (creatureNode.Hitbox != null)
		{
			Vector2 globalPosition = creatureNode.Hitbox.GlobalPosition;
			Vector2 size = creatureNode.Hitbox.Size;
			if (size.X > 5f && size.Y > 5f)
			{
				return new Rect2(globalPosition, size);
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
			return new Rect2(val4, val);
		}
		Vector2 val5 = ((Control)creatureNode).GlobalPosition - Vector2.One * 100f;
		GD.PrintErr("[SplitViewport] 无法获取精确边界，使用降级默认值");
		return new Rect2(val5, Vector2.One * 200f);
	}

	private static TextureRect CreateTextureRect(Image image, Vector2 size)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		TextureRect val = new TextureRect();
		val.Texture = (Texture2D)(object)ImageTexture.CreateFromImage(image);
		((Control)val).Size = size;
		((Control)val).MouseFilter = (MouseFilterEnum)2;
		return val;
	}

	private static void SplitImageDiagonal(Image src, out Image left, out Image right, Vector2 center, float angleDeg, bool showWound, float woundThickness, float? woundStartX = null, float? woundEndX = null, float? woundStartY = null, float? woundEndY = null)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Invalid comparison between Unknown and I8
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		int width = src.GetWidth();
		int height = src.GetHeight();
		float num = angleDeg * (float)Math.PI / 180f;
		float num2 = Mathf.Cos(num);
		float num3 = Mathf.Sin(num);
		float d = 0f - (num2 * center.X + num3 * center.Y);
		if ((long)src.GetFormat() == 5)
		{
			SplitImageDiagonalFast(src, width, height, num2, num3, d, showWound, woundThickness, woundStartX, woundEndX, woundStartY, woundEndY, out left, out right);
			return;
		}
		left = Image.Create(width, height, false, src.GetFormat());
		right = Image.Create(width, height, false, src.GetFormat());
		SplitImageDiagonalSlow(src, left, right, width, height, num2, num3, d, showWound, woundThickness, woundStartX, woundEndX, woundStartY, woundEndY);
	}

	private static void SplitImageDiagonalFast(Image src, int w, int h, float nx, float ny, float d, bool showWound, float woundThickness, float? woundStartX, float? woundEndX, float? woundStartY, float? woundEndY, out Image left, out Image right)
	{
		byte[] data = src.GetData();
		byte[] array = new byte[w * h * 4];
		byte[] array2 = new byte[w * h * 4];
		byte b = (byte)(WOUND_COLOR.R * 255f);
		byte b2 = (byte)(WOUND_COLOR.G * 255f);
		byte b3 = (byte)(WOUND_COLOR.B * 255f);
		byte b4 = (byte)(WOUND_COLOR.A * 255f);
		for (int i = 0; i < h; i++)
		{
			int num = i * w * 4;
			for (int j = 0; j < w; j++)
			{
				int num2 = num + j * 4;
				float num3 = nx * (float)j + ny * (float)i + d;
				bool flag = num3 >= 0f;
				bool flag2 = false;
				if (showWound && Math.Abs(num3) <= woundThickness)
				{
					flag2 = true;
					if (woundStartX.HasValue && (float)j < woundStartX.Value)
					{
						flag2 = false;
					}
					if (woundEndX.HasValue && (float)j > woundEndX.Value)
					{
						flag2 = false;
					}
					if (woundStartY.HasValue && (float)i < woundStartY.Value)
					{
						flag2 = false;
					}
					if (woundEndY.HasValue && (float)i > woundEndY.Value)
					{
						flag2 = false;
					}
				}
				byte b5;
				byte b6;
				byte b7;
				byte b8;
				if (flag2)
				{
					b5 = b;
					b6 = b2;
					b7 = b3;
					b8 = b4;
				}
				else
				{
					b5 = data[num2];
					b6 = data[num2 + 1];
					b7 = data[num2 + 2];
					b8 = data[num2 + 3];
				}
				if (flag)
				{
					array2[num2] = b5;
					array2[num2 + 1] = b6;
					array2[num2 + 2] = b7;
					array2[num2 + 3] = b8;
				}
				else
				{
					array[num2] = b5;
					array[num2 + 1] = b6;
					array[num2 + 2] = b7;
					array[num2 + 3] = b8;
				}
			}
		}
		left = Image.CreateFromData(w, h, false, (Format)5, array);
		right = Image.CreateFromData(w, h, false, (Format)5, array2);
	}

	private static void SplitImageDiagonalSlow(Image src, Image left, Image right, int w, int h, float nx, float ny, float d, bool showWound, float woundThickness, float? woundStartX, float? woundEndX, float? woundStartY, float? woundEndY)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < h; i++)
		{
			for (int j = 0; j < w; j++)
			{
				float num = nx * (float)j + ny * (float)i + d;
				Color val = src.GetPixel(j, i);
				if (showWound)
				{
					bool flag = true;
					if (woundStartX.HasValue && (float)j < woundStartX.Value)
					{
						flag = false;
					}
					if (woundEndX.HasValue && (float)j > woundEndX.Value)
					{
						flag = false;
					}
					if (woundStartY.HasValue && (float)i < woundStartY.Value)
					{
						flag = false;
					}
					if (woundEndY.HasValue && (float)i > woundEndY.Value)
					{
						flag = false;
					}
					if (flag && Math.Abs(num) <= woundThickness)
					{
						val = WOUND_COLOR;
					}
				}
				if (num >= 0f)
				{
					right.SetPixel(j, i, val);
				}
				else
				{
					left.SetPixel(j, i, val);
				}
			}
		}
	}
}
