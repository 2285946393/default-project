using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class DistortionFilter
{
	private static ColorRect _filter;

	private static ShaderMaterial _material;

	private static float _waveRadius = 0.16f;

	private static bool _isActive;

	private const string ShaderCode = "\r\nshader_type canvas_item;\r\nuniform sampler2D screen_tex : hint_screen_texture, filter_linear_mipmap;\r\nuniform float amount = 0.0;\r\nuniform vec2 wave_center = vec2(0.3, 0.55);\r\nuniform float wave_radius = 0.0;\r\nuniform float wave_width = 0.045;\r\nuniform float wave_strength = 0.045;\r\n\r\nfloat ring(float r, float d, float width) {\r\n    if (d < 0.0) return 0.0;\r\n    float x = (r - d) / width;\r\n    return exp(-x * x) * smoothstep(0.12, 0.20, d) / (1.0 + 2.0 * d);\r\n}\r\n\r\nvoid fragment() {\r\n    float aspect = SCREEN_PIXEL_SIZE.y / SCREEN_PIXEL_SIZE.x;\r\n    vec2 p = SCREEN_UV - wave_center;\r\n    p.x *= aspect;\r\n    float r = length(p);\r\n    float band = ring(r, wave_radius, wave_width);\r\n    band = min(band, 1.2);\r\n    vec2 dirv = (r > 0.0001) ? p / r : vec2(0.0);\r\n    vec2 off = dirv * band * wave_strength * amount;\r\n    off.x /= aspect;\r\n\r\n    vec2 uv = SCREEN_UV + off;\r\n    uv += vec2(sin(uv.y * 9.0 + TIME * 0.6), sin(uv.x * 8.0 - TIME * 0.5)) * 0.0022 * amount;\r\n    uv.x += sin(uv.y * 34.0 + TIME * 1.3) * 0.0009 * amount;\r\n    float ab = 0.0016 * amount;\r\n    float cr = texture(screen_tex, uv - vec2(ab, 0.0)).r;\r\n    vec4 mid = texture(screen_tex, uv);\r\n    float cb = texture(screen_tex, uv + vec2(ab, 0.0)).b;\r\n    vec3 col = vec3(cr, mid.g, cb);\r\n    float luma = dot(col, vec3(0.299, 0.587, 0.114));\r\n    vec3 cool = mix(col, vec3(luma), 0.55) * vec3(0.80, 0.94, 1.22);\r\n    float d = distance(SCREEN_UV, vec2(0.5));\r\n    cool *= 1.0 - 0.22 * smoothstep(0.40, 0.85, d);\r\n    cool = mix(cool, cool * vec3(0.85, 1.05, 1.25) + vec3(0.02, 0.06, 0.12), min(band, 1.0) * 0.4);\r\n    COLOR = vec4(mix(texture(screen_tex, SCREEN_UV).rgb, cool, amount), 1.0);\r\n}";

	private static readonly Shader _sharedShader = new Shader
	{
		Code = "\r\nshader_type canvas_item;\r\nuniform sampler2D screen_tex : hint_screen_texture, filter_linear_mipmap;\r\nuniform float amount = 0.0;\r\nuniform vec2 wave_center = vec2(0.3, 0.55);\r\nuniform float wave_radius = 0.0;\r\nuniform float wave_width = 0.045;\r\nuniform float wave_strength = 0.045;\r\n\r\nfloat ring(float r, float d, float width) {\r\n    if (d < 0.0) return 0.0;\r\n    float x = (r - d) / width;\r\n    return exp(-x * x) * smoothstep(0.12, 0.20, d) / (1.0 + 2.0 * d);\r\n}\r\n\r\nvoid fragment() {\r\n    float aspect = SCREEN_PIXEL_SIZE.y / SCREEN_PIXEL_SIZE.x;\r\n    vec2 p = SCREEN_UV - wave_center;\r\n    p.x *= aspect;\r\n    float r = length(p);\r\n    float band = ring(r, wave_radius, wave_width);\r\n    band = min(band, 1.2);\r\n    vec2 dirv = (r > 0.0001) ? p / r : vec2(0.0);\r\n    vec2 off = dirv * band * wave_strength * amount;\r\n    off.x /= aspect;\r\n\r\n    vec2 uv = SCREEN_UV + off;\r\n    uv += vec2(sin(uv.y * 9.0 + TIME * 0.6), sin(uv.x * 8.0 - TIME * 0.5)) * 0.0022 * amount;\r\n    uv.x += sin(uv.y * 34.0 + TIME * 1.3) * 0.0009 * amount;\r\n    float ab = 0.0016 * amount;\r\n    float cr = texture(screen_tex, uv - vec2(ab, 0.0)).r;\r\n    vec4 mid = texture(screen_tex, uv);\r\n    float cb = texture(screen_tex, uv + vec2(ab, 0.0)).b;\r\n    vec3 col = vec3(cr, mid.g, cb);\r\n    float luma = dot(col, vec3(0.299, 0.587, 0.114));\r\n    vec3 cool = mix(col, vec3(luma), 0.55) * vec3(0.80, 0.94, 1.22);\r\n    float d = distance(SCREEN_UV, vec2(0.5));\r\n    cool *= 1.0 - 0.22 * smoothstep(0.40, 0.85, d);\r\n    cool = mix(cool, cool * vec3(0.85, 1.05, 1.25) + vec3(0.02, 0.06, 0.12), min(band, 1.0) * 0.4);\r\n    COLOR = vec4(mix(texture(screen_tex, SCREEN_UV).rgb, cool, amount), 1.0);\r\n}"
	};

	public static void Create(NCombatRoom room, Creature playerCreature)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Expected O, but got Unknown
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		if (_filter == null)
		{
			_isActive = true;
			_waveRadius = 0.16f;
			_material = new ShaderMaterial
			{
				Shader = _sharedShader
			};
			_material.SetShaderParameter(StringName.op_Implicit("amount"), Variant.op_Implicit(0.8f));
			Vector2 val = default(Vector2);
			((Vector2)(ref val))._002Ector(0.3f, 0.55f);
			NCreature creatureNode = playerCreature.GetCreatureNode();
			if (creatureNode != null)
			{
				Control combatVfxContainer = room.CombatVfxContainer;
				Vector2 val2 = ((CanvasItem)combatVfxContainer).GetGlobalTransformWithCanvas() * (creatureNode.VfxSpawnPosition - combatVfxContainer.GlobalPosition);
				Rect2 viewportRect = ((CanvasItem)room).GetViewportRect();
				((Vector2)(ref val))._002Ector(Mathf.Clamp(val2.X / ((Rect2)(ref viewportRect)).Size.X, 0f, 1f), Mathf.Clamp(val2.Y / ((Rect2)(ref viewportRect)).Size.Y, 0f, 1f));
			}
			_material.SetShaderParameter(StringName.op_Implicit("wave_center"), Variant.op_Implicit(val));
			Rect2 viewportRect2 = ((CanvasItem)room).GetViewportRect();
			_filter = new ColorRect
			{
				Material = (Material)(object)_material,
				Color = Colors.White,
				Size = ((Rect2)(ref viewportRect2)).Size * 2f,
				ZIndex = 0,
				MouseFilter = (MouseFilterEnum)2
			};
			((Control)_filter).GlobalPosition = -((Rect2)(ref viewportRect2)).Size * 0.5f;
			((Node)room.CombatVfxContainer).AddChild((Node)(object)_filter, false, (InternalMode)0);
			UpdateWaveLoop();
		}
	}

	private static async Task UpdateWaveLoop()
	{
		float elapsed = 0f;
		while (_isActive && elapsed < 1f)
		{
			_waveRadius += 0.02f;
			if (_waveRadius > 2.2f)
			{
				_waveRadius = 0.16f;
			}
			ShaderMaterial material = _material;
			if (material != null)
			{
				material.SetShaderParameter(StringName.op_Implicit("wave_radius"), Variant.op_Implicit(_waveRadius));
			}
			await Cmd.Wait(0.016f, true);
			elapsed += 0.016f;
		}
		FadeOut();
	}

	public static void FadeOut()
	{
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		_isActive = false;
		if (_filter != null && GodotObject.IsInstanceValid((GodotObject)(object)_filter))
		{
			Tween val = ((Node)_filter).CreateTween();
			if (_material != null)
			{
				val.TweenProperty((GodotObject)(object)_material, NodePath.op_Implicit("shader_parameter/amount"), Variant.op_Implicit(0f), 0.25);
			}
			else
			{
				val.TweenProperty((GodotObject)(object)_filter, NodePath.op_Implicit("color:a"), Variant.op_Implicit(0f), 0.25);
			}
			val.TweenCallback(Callable.From((Action)((Node)_filter).QueueFree));
		}
		_filter = null;
		_material = null;
	}
}
