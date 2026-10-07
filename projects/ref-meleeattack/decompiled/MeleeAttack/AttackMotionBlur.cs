using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class AttackMotionBlur
{
	private class TimeWrapper
	{
		public long Value;
	}

	[HarmonyPatch(typeof(CreatureCmd), "TriggerAnim", new Type[]
	{
		typeof(Creature),
		typeof(string),
		typeof(float)
	})]
	private static class BlurTriggerAnimPatch
	{
		private static void Prefix(Creature creature, string triggerName, float waitTime)
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Invalid comparison between Unknown and I4
			if (creature == null || (int)creature.Side != 1 || string.IsNullOrEmpty(triggerName) || !triggerName.StartsWith("Attack", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
			string creatureId = SimpleTeleportPatch.GetCreatureId(creature);
			if (!SettingsUI.IsTeleportEnabledForCharacter(creatureId))
			{
				return;
			}
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance != null)
			{
				NCreature creatureNode = instance.GetCreatureNode(creature);
				if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
				{
					ApplyBlurToTarget(creatureNode);
				}
			}
		}
	}

	private const float BLUR_PEAK_STRENGTH = 0.06f;

	private const float BOUNDS_SCALE = 2f;

	private const int BLUR_SAMPLES = 16;

	private const float FADE_IN_SECONDS = 0.03f;

	private const float HOLD_SECONDS = 0.08f;

	private const float FADE_OUT_SECONDS = 0.08f;

	private const long BLUR_COOLDOWN_MS = 300L;

	private static readonly string ShaderCode = $"\r\nshader_type canvas_item;\r\n\r\nuniform sampler2D sprite_tex : filter_linear;\r\nuniform float strength = {0.06f};\r\nuniform float alpha = 0.0;\r\n\r\nconst int SAMPLES = {16};\r\n\r\nvoid fragment() {{\r\n    vec2 uv = UV;\r\n    float half_count = float(SAMPLES - 1) * 0.5;\r\n\r\n    vec4 sum = vec4(0.0);\r\n    for (int i = 0; i < SAMPLES; i++) {{\r\n        float t = (float(i) - half_count) / half_count;\r\n        vec2 offset = vec2(t * strength, 0.0);\r\n        vec2 sample_uv = clamp(uv + offset, vec2(0.0), vec2(1.0));\r\n        sum += texture(sprite_tex, sample_uv);\r\n    }}\r\n\r\n    vec4 col = sum / float(SAMPLES);\r\n    COLOR = vec4(col.rgb, col.a * alpha);\r\n}}\r\n";

	private static readonly Shader _sharedShader = new Shader
	{
		Code = ShaderCode
	};

	private static readonly ConditionalWeakTable<NCreature, TimeWrapper> _lastBlurTime = new ConditionalWeakTable<NCreature, TimeWrapper>();

	public static void ApplyBlurToTarget(NCreature targetNode)
	{
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Expected O, but got Unknown
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Expected O, but got Unknown
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Expected O, but got Unknown
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_029f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0301: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0318: Expected O, but got Unknown
		//IL_0349: Unknown result type (might be due to invalid IL or missing references)
		//IL_0388: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
		if (targetNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)targetNode))
		{
			return;
		}
		long ticksMsec = (long)Time.GetTicksMsec();
		if (_lastBlurTime.TryGetValue(targetNode, out var value))
		{
			if (ticksMsec - value.Value < 300)
			{
				return;
			}
			value.Value = ticksMsec;
		}
		else
		{
			_lastBlurTime.Add(targetNode, new TimeWrapper
			{
				Value = ticksMsec
			});
		}
		Node2D body = targetNode.Body;
		if (body == null || !GodotObject.IsInstanceValid((GodotObject)(object)body))
		{
			return;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return;
		}
		Rect2 visualBounds = GetVisualBounds(targetNode);
		if (((Rect2)(ref visualBounds)).Size.X < 5f || ((Rect2)(ref visualBounds)).Size.Y < 5f)
		{
			return;
		}
		Vector2 val = ((Rect2)(ref visualBounds)).Position + ((Rect2)(ref visualBounds)).Size * 0.5f;
		Vector2 val2 = ((Rect2)(ref visualBounds)).Size * 2f;
		Vector2 val3 = val - val2 * 0.5f;
		Rect2 val4 = default(Rect2);
		((Rect2)(ref val4))._002Ector(val3, val2);
		Vector2I size = default(Vector2I);
		((Vector2I)(ref size))._002Ector(Mathf.Max(1, (int)((Rect2)(ref val4)).Size.X), Mathf.Max(1, (int)((Rect2)(ref val4)).Size.Y));
		SubViewport spriteVp = new SubViewport
		{
			Size = size,
			TransparentBg = true,
			Disable3D = true,
			RenderTargetUpdateMode = (UpdateMode)1,
			RenderTargetClearMode = (ClearMode)0
		};
		Node2D val5 = (Node2D)((Node)body).Duplicate(1);
		if (val5 == null)
		{
			((Node)spriteVp).QueueFree();
			return;
		}
		SyncSpineAnimationToCopy(targetNode, val5);
		Vector2 center = ((Rect2)(ref visualBounds)).GetCenter();
		Vector2 globalPosition = body.GlobalPosition;
		Vector2 val6 = center - globalPosition;
		val5.Position = ((Rect2)(ref val4)).Size * 0.5f - val6;
		DisableAllProcessing((Node)(object)val5);
		((Node)spriteVp).AddChild((Node)(object)val5, false, (InternalMode)0);
		((Node)instance).AddChild((Node)(object)spriteVp, false, (InternalMode)0);
		ShaderMaterial val7 = new ShaderMaterial
		{
			Shader = _sharedShader
		};
		val7.SetShaderParameter(StringName.op_Implicit("sprite_tex"), Variant.op_Implicit((GodotObject)(object)((Viewport)spriteVp).GetTexture()));
		val7.SetShaderParameter(StringName.op_Implicit("strength"), Variant.op_Implicit(0.06f));
		val7.SetShaderParameter(StringName.op_Implicit("alpha"), Variant.op_Implicit(0f));
		TextureRect displayRect = new TextureRect
		{
			Material = (Material)(object)val7,
			Texture = (Texture2D)(object)((Viewport)spriteVp).GetTexture(),
			Size = ((Rect2)(ref val4)).Size,
			GlobalPosition = ((Rect2)(ref val4)).Position,
			MouseFilter = (MouseFilterEnum)2,
			ZIndex = 100,
			ZAsRelative = false,
			ExpandMode = (ExpandModeEnum)1,
			StretchMode = (StretchModeEnum)0
		};
		((Node)instance).AddChild((Node)(object)displayRect, false, (InternalMode)0);
		Tween val8 = ((Node)displayRect).CreateTween();
		val8.TweenProperty((GodotObject)(object)val7, NodePath.op_Implicit("shader_parameter/alpha"), Variant.op_Implicit(1f), 0.029999999329447746).SetTrans((TransitionType)0);
		val8.TweenInterval(0.07999999821186066);
		val8.TweenProperty((GodotObject)(object)val7, NodePath.op_Implicit("shader_parameter/alpha"), Variant.op_Implicit(0f), 0.07999999821186066).SetTrans((TransitionType)0);
		val8.TweenCallback(Callable.From((Action)delegate
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)displayRect))
			{
				((Node)displayRect).QueueFree();
			}
			if (GodotObject.IsInstanceValid((GodotObject)(object)spriteVp))
			{
				((Node)spriteVp).QueueFree();
			}
		}));
	}

	private static void DisableAllProcessing(Node node)
	{
		node.SetProcess(false);
		node.SetPhysicsProcess(false);
		node.SetProcessInput(false);
		node.SetProcessUnhandledInput(false);
		node.SetProcessUnhandledKeyInput(false);
		foreach (Node child in node.GetChildren(false))
		{
			DisableAllProcessing(child);
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
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
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
		return new Rect2(val5, Vector2.One * 200f);
	}

	private static void SyncSpineAnimationToCopy(NCreature source, Node2D copy)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Expected O, but got Unknown
		SpineAnimationAccess spineAnimation = source.SpineAnimation;
		if (!((SpineAnimationAccess)(ref spineAnimation)).IsValid)
		{
			return;
		}
		MegaTrackEntry currentTrack = ((SpineAnimationAccess)(ref spineAnimation)).GetCurrentTrack(0);
		if (currentTrack == null)
		{
			return;
		}
		string animName = currentTrack.GetAnimationName();
		float animTime = currentTrack.GetTrackTime();
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
				method.Invoke(state, new object[3] { animName, false, 0 });
			}
			else
			{
				((object)state).GetType().GetMethod("SetAnimation", new Type[2]
				{
					typeof(string),
					typeof(bool)
				})?.Invoke(state, new object[2] { animName, false });
			}
			MegaTrackEntry current = state.GetCurrent(0);
			if (current != null)
			{
				current.SetTrackTime(animTime);
				((MegaSpineBinding)current).Dispose();
			}
			state.SetTimeScale(0f);
		});
	}
}
