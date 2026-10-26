using System;
using System.Collections.Generic;
using System.Linq;
using IntentGraph2.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace IntentGraph2.DevConsole;

public class EditIntentConsoleCmd : AbstractConsoleCmd
{
	public override string CmdName => "editintent";

	public override string Args => "<monster model full name>";

	public override string Description => "Open the intent graph editor for the given monster model in the current combat";

	public override bool IsNetworked => false;

	public override CmdResult Process(Player? issuingPlayer, string[] args)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		string monsterModelFullName = string.Join(" ", args).Trim();
		if (string.IsNullOrWhiteSpace(monsterModelFullName))
		{
			return new CmdResult(false, "Monster model full name is required.");
		}
		CombatState val = CombatManager.Instance.DebugOnlyGetState();
		if (val == null || val.Encounter == null)
		{
			return new CmdResult(false, "You must be in combat to edit an intent graph.");
		}
		Creature val2 = ((IEnumerable<Creature>)val.Enemies).FirstOrDefault((Func<Creature, bool>)delegate(Creature enemy)
		{
			MonsterModel monster = enemy.Monster;
			return monster != null && (((object)monster).GetType().FullName?.Equals(monsterModelFullName, StringComparison.OrdinalIgnoreCase)).GetValueOrDefault();
		});
		if (((val2 != null) ? val2.Monster : null) == null)
		{
			string[] array = (from name in (from enemy in val.Enemies
					select ((object)enemy.Monster)?.GetType().FullName into name
					where !string.IsNullOrWhiteSpace(name)
					select name).Distinct<string>(StringComparer.OrdinalIgnoreCase)
				orderby name
				select name).ToArray();
			string text = ((array.Length == 0) ? ("Monster model '" + monsterModelFullName + "' is not active in this combat.") : ("Monster model '" + monsterModelFullName + "' is not active in this combat. Active models: " + string.Join(", ", array)));
			return new CmdResult(false, text);
		}
		if (!IntentGraphEditorHost.TryOpenEditor(val2.Monster, val2.Name, out string message))
		{
			return new CmdResult(false, message);
		}
		return new CmdResult(true, message);
	}

	public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
	{
		if (args.Length <= 1)
		{
			return ((AbstractConsoleCmd)this).CompleteArgument((IEnumerable<string>)GetAvailableMonsterModelNames(), Array.Empty<string>(), args.FirstOrDefault() ?? string.Empty, (CompletionType)2, (Func<string, string, bool>)null);
		}
		return ((AbstractConsoleCmd)this).GetArgumentCompletions(player, args);
	}

	private static string[] GetAvailableMonsterModelNames()
	{
		CombatState val = CombatManager.Instance.DebugOnlyGetState();
		if (((val != null) ? val.Encounter : null) == null)
		{
			return Array.Empty<string>();
		}
		return (from enemy in val.Enemies
			select ((object)enemy.Monster)?.GetType().FullName into name
			where !string.IsNullOrWhiteSpace(name)
			select name).Distinct<string>(StringComparer.OrdinalIgnoreCase).OrderBy<string, string>((string name) => name, StringComparer.OrdinalIgnoreCase).ToArray();
	}
}
