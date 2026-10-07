using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RegentFX.Scripts.Vfx;

public abstract class FX : IWithFxLoad
{
	public const string DISTORTION = "res://RegentFX/scenes/vfx/distortions/vfx_outward_screen_distortion_ellipse.tscn";

	public virtual string? VfxScenePath => null;

	public virtual List<string> AssetPaths
	{
		get
		{
			if (VfxScenePath != null)
			{
				int num = 1;
				List<string> list = new List<string>(num);
				CollectionsMarshal.SetCount(list, num);
				CollectionsMarshal.AsSpan(list)[0] = VfxScenePath;
				return list;
			}
			return new List<string>();
		}
	}
}
