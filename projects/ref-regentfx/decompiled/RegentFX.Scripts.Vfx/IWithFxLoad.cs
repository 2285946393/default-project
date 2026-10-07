using System.Collections.Generic;

namespace RegentFX.Scripts.Vfx;

public interface IWithFxLoad
{
	List<string> AssetPaths { get; }
}
