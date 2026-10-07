using System;
using System.Threading.Tasks;
using Godot;

namespace MeleeAttack;

public static class HitStop
{
	public static async Task Apply(float duration = 0.2f)
	{
		if (SettingsUI.IsFastModeEnabled() || Engine.TimeScale < 0.009999999776482582)
		{
			return;
		}
		float originalTimeScale = (float)Engine.TimeScale;
		SimpleTeleportPatch.AddReturnDelayWindow(duration);
		MainLoop mainLoop = Engine.GetMainLoop();
		SceneTree sceneTree = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
		if (sceneTree == null || sceneTree.Root == null)
		{
			GD.PrintErr("[HitStop] SceneTree 无效，返回");
			return;
		}
		Node guardNode = new Node();
		guardNode.Name = StringName.op_Implicit("HitStopGuard");
		((Node)sceneTree.Root).AddChild(guardNode, false, (InternalMode)0);
		bool finished = false;
		long startMs = (long)Time.GetTicksMsec();
		long durationMs = (long)(duration * 1000f);
		Action processHandler = delegate
		{
			if (!finished)
			{
				if (Math.Abs(Engine.TimeScale - 0.0) > 0.0010000000474974513)
				{
					Engine.TimeScale = 0.0;
				}
				if ((long)Time.GetTicksMsec() - startMs >= durationMs)
				{
					finished = true;
					if (GodotObject.IsInstanceValid((GodotObject)(object)guardNode))
					{
						guardNode.QueueFree();
					}
				}
			}
		};
		sceneTree.ProcessFrame += processHandler;
		try
		{
			Engine.TimeScale = 0.0;
			Timer timer = new Timer();
			timer.WaitTime = duration + 0.1f;
			timer.OneShot = true;
			timer.Timeout += delegate
			{
				if (!finished)
				{
					finished = true;
					if (GodotObject.IsInstanceValid((GodotObject)(object)guardNode))
					{
						guardNode.QueueFree();
					}
				}
			};
			guardNode.AddChild((Node)(object)timer, false, (InternalMode)0);
			timer.Start(-1.0);
			while (!finished)
			{
				await ((GodotObject)sceneTree).ToSignal((GodotObject)(object)sceneTree, SignalName.ProcessFrame);
			}
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			GD.PrintErr("[HitStop] 异常: " + ex.Message);
		}
		finally
		{
			sceneTree.ProcessFrame -= processHandler;
			if (GodotObject.IsInstanceValid((GodotObject)(object)guardNode))
			{
				guardNode.QueueFree();
			}
			Engine.TimeScale = originalTimeScale;
			await ((GodotObject)sceneTree).ToSignal((GodotObject)(object)sceneTree, SignalName.ProcessFrame);
			if (Math.Abs(Engine.TimeScale - (double)originalTimeScale) > 0.0010000000474974513)
			{
				Engine.TimeScale = originalTimeScale;
			}
		}
	}
}
