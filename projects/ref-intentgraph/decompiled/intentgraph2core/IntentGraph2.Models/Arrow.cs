using System;
using System.Linq;

namespace IntentGraph2.Models;

public record Arrow(float[] Path, string? RelativeTo = null)
{
	public override int GetHashCode()
	{
		HashCode hashCode = default(HashCode);
		float[] path = Path;
		foreach (float value in path)
		{
			hashCode.Add(value);
		}
		hashCode.Add(RelativeTo);
		return hashCode.ToHashCode();
	}

	public virtual bool Equals(Arrow? obj)
	{
		if (obj == null || obj.Path.Length != Path.Length || obj.RelativeTo != RelativeTo)
		{
			return false;
		}
		if (!obj.Path.SequenceEqual(Path))
		{
			return false;
		}
		return true;
	}
}
