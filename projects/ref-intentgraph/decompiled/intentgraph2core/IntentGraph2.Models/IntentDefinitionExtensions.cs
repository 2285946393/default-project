using System.Collections.Generic;

namespace IntentGraph2.Models;

public static class IntentDefinitionExtensions
{
	public static MoveReplacement? GetMoveReplacementOrNull(this Dictionary<string, MoveReplacement>? moveReplacements, string stateId, string? nodeFullId)
	{
		if (moveReplacements == null)
		{
			return null;
		}
		if (nodeFullId != null && moveReplacements.TryGetValue(nodeFullId, out MoveReplacement value))
		{
			return value;
		}
		moveReplacements.TryGetValue(stateId, out MoveReplacement value2);
		return value2;
	}
}
