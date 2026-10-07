using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace RegentFX.Scripts.Patches;

[HarmonyPatch]
public static class StarryImpactPatch
{
	private static HashSet<string> exceptNodes = new HashSet<string> { "vfx_starry_impact_smoke_flipbook", "vfx_outward_screen_distortion" };

	[HarmonyPatch(typeof(NStarryImpactVfx), "PlaySequence")]
	[HarmonyPrefix]
	public static bool p(NStarryImpactVfx __instance, ref Task __result)
	{
		if (Entry.StarEffectController != null)
		{
			ulong instanceId = ((GodotObject)__instance).GetInstanceId();
			if (VFXUtil.StarryImpactNodes.Contains(instanceId))
			{
				__result = MyTask(__instance);
				VFXUtil.StarryImpactNodes.Remove(instanceId);
				return false;
			}
		}
		return true;
	}

	private static async Task MyTask(NStarryImpactVfx node)
	{
		node._cts = new CancellationTokenSource();
		foreach (GpuParticles2D particle in node._particles)
		{
			if (exceptNodes.Contains(StringName.op_Implicit(((Node)particle).Name)))
			{
				GodotTreeExtensions.QueueFreeSafely((Node)(object)particle);
				continue;
			}
			ParticleProcessMaterial val = (ParticleProcessMaterial)((Resource)particle.ProcessMaterial).Duplicate(false);
			val.Scale *= 0.2f;
			particle.ProcessMaterial = (Material)(object)val;
			particle.Restart();
		}
		await VFXUtil.Wait(2f, node._cts.Token);
		if (node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			GodotTreeExtensions.QueueFreeSafely((Node)(object)node);
		}
	}
}
