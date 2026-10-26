using IntentGraph2.Utils;

namespace IntentGraph2.Crossovers;

public interface IBaseLibHelper
{
	IntentGraphModConfig Config { get; }

	void RegisterConfig();

	void SaveConfig();
}
