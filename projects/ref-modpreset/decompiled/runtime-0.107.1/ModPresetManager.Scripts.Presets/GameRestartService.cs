using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Godot;
using MegaCrit.Sts2.Core.Nodes;

namespace ModPresetManager.Scripts.Presets;

internal static class GameRestartService
{
	public static bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

	public static bool TryRestart(out string? error)
	{
		error = null;
		if (!IsSupported)
		{
			error = "unsupported_platform";
			return false;
		}
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = "cmd.exe",
				Arguments = "/c timeout /t 2 /nobreak >nul & start \"\" \"steam://rungameid/2868840\"",
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				UseShellExecute = false
			});
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
		if (NGame.Instance != null)
		{
			NGame.Instance.Quit();
		}
		else
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val != null)
			{
				val.Quit(0);
			}
		}
		return true;
	}
}
