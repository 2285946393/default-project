using System.Collections.Generic;

namespace IntentGraph2.Models;

public class IntentDefinition
{
	public string Condition { get; set; } = "true";


	public string? UpToDateCondition { get; set; }

	public string? AlternativeMonsterId { get; set; }

	public SecondaryInitialState[]? SecondaryInitialStates { get; set; }

	public Graph? Graph { get; set; }

	public Graph? GraphPatch { get; set; }

	public StateMachineNode[]? StateMachine { get; set; }

	public Dictionary<string, MoveReplacement>? MoveReplacements { get; set; }

	public Position Offset { get; set; }
}
