namespace IntentGraph2.Models;

public interface IRelativeToPosition
{
	float X { get; }

	float Y { get; }

	string? RelativeTo { get; }
}
