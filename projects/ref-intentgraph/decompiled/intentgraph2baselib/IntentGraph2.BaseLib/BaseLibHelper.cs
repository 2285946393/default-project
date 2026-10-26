using BaseLib.Config;
using IntentGraph2.Crossovers;
using IntentGraph2.Utils;

namespace IntentGraph2.BaseLib;

public class BaseLibHelper : IBaseLibHelper
{
	public IntentGraphModConfig Config => HelperModConfig.BaseConfig;

	public void RegisterConfig()
	{
		ModConfigRegistry.Register("intentgraph2", (ModConfig)(object)new HelperModConfig());
	}

	public void SaveConfig()
	{
		ModConfig.SaveDebounced<HelperModConfig>(1000);
	}
}
