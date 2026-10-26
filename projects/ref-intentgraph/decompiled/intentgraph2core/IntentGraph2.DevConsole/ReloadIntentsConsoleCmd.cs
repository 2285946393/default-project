using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;

namespace IntentGraph2.DevConsole;

public class ReloadIntentsConsoleCmd : AbstractConsoleCmd
{
	public override string CmdName => "reloadintents";

	public override string Args => string.Empty;

	public override string Description => "Reload intent graph for developing";

	public override bool IsNetworked => false;

	public override CmdResult Process(Player? issuingPlayer, string[] args)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		IntentGraphMod.ReloadIntentDefinitionsAndGraphs();
		return new CmdResult(true, "Intent graph reloaded");
	}
}
