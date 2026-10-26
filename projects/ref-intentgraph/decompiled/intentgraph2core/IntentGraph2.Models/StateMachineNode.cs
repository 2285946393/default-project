namespace IntentGraph2.Models;

public class StateMachineNode
{
	public string Name { get; set; } = string.Empty;


	public string? MoveName { get; set; }

	public string[]? AlternativeMoveNames { get; set; }

	public bool IsInitialState { get; set; }

	public int InitialStatePriority { get; set; }

	public StateMachinNodeChildren[]? Children { get; set; }

	public string? FollowUpState { get; set; }

	public bool HorizontalLayout { get; set; }

	public int PlaceholderIntentCount { get; set; }

	public bool NotSimpleLoopStart { get; set; }

	public Position Offset { get; set; }
}
