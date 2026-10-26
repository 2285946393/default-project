using System;
using System.Linq;
using System.Text.Json.Serialization;
using IntentGraph2.Utils.Expression;

namespace IntentGraph2.Models;

public record Move(string Id, string[]? Ids = null, float X = 0f, float Y = 0f, Icon[]? Icons = null, int?[]? PossiblePreviousMoveNodeIndices = null, string? RelativeTo = null, [property: JsonIgnore] IExpression? CurrentMoveCondition = null) : IRelativeToPosition
{
	public override int GetHashCode()
	{
		HashCode hashCode = default(HashCode);
		hashCode.Add(Id);
		hashCode.Add(X);
		hashCode.Add(Y);
		if (Ids != null)
		{
			string[] ids = Ids;
			foreach (string value in ids)
			{
				hashCode.Add(value);
			}
		}
		if (Icons != null)
		{
			Icon[] icons = Icons;
			foreach (Icon value2 in icons)
			{
				hashCode.Add(value2);
			}
		}
		if (PossiblePreviousMoveNodeIndices != null)
		{
			int?[] possiblePreviousMoveNodeIndices = PossiblePreviousMoveNodeIndices;
			foreach (int? value3 in possiblePreviousMoveNodeIndices)
			{
				hashCode.Add(value3);
			}
		}
		return hashCode.ToHashCode();
	}

	public virtual bool Equals(Move? obj)
	{
		if (obj == null || obj.Id != Id || obj.X != X || obj.Y != Y || obj.RelativeTo != RelativeTo)
		{
			return false;
		}
		if (Ids?.Length != obj.Ids?.Length || Icons?.Length != obj.Icons?.Length || PossiblePreviousMoveNodeIndices?.Length != obj.PossiblePreviousMoveNodeIndices?.Length)
		{
			return false;
		}
		if (Ids != null && !obj.Ids.SequenceEqual(Ids))
		{
			return false;
		}
		if (Icons != null && !obj.Icons.SequenceEqual(Icons))
		{
			return false;
		}
		if (PossiblePreviousMoveNodeIndices != null && !obj.PossiblePreviousMoveNodeIndices.SequenceEqual(PossiblePreviousMoveNodeIndices))
		{
			return false;
		}
		return true;
	}
}
