using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace IntentGraph2.Models;

public class Graph
{
	public float Width { get; set; } = 1f;


	public float Height { get; set; } = 1f;


	public List<Icon> Icons { get; set; } = new List<Icon>();


	public List<IconGroup> IconGroups { get; set; } = new List<IconGroup>();


	public List<Label> Labels { get; set; } = new List<Label>();


	public List<Arrow> Arrows { get; set; } = new List<Arrow>();


	public List<Move> Moves { get; set; } = new List<Move>();


	[JsonIgnore]
	public string? Warning { get; set; }

	[JsonIgnore]
	public IntentDefinition? IntentDefinition { get; set; }

	public bool Expand { get; set; }

	public override int GetHashCode()
	{
		HashCode hashCode = default(HashCode);
		hashCode.Add(Width);
		hashCode.Add(Height);
		Icons.ForEach(((HashCode)hashCode).Add);
		IconGroups.ForEach(((HashCode)hashCode).Add);
		Labels.ForEach(((HashCode)hashCode).Add);
		Arrows.ForEach(((HashCode)hashCode).Add);
		Moves.ForEach(((HashCode)hashCode).Add);
		return hashCode.ToHashCode();
	}

	public override bool Equals(object? obj)
	{
		if (obj?.GetType() != typeof(Graph))
		{
			return false;
		}
		Graph graph = (Graph)obj;
		if (graph.Width != Width || graph.Height != Height)
		{
			return false;
		}
		if (graph.Icons.Count != Icons.Count || graph.IconGroups.Count != IconGroups.Count || graph.Labels.Count != Labels.Count || graph.Arrows.Count != Arrows.Count || graph.Moves.Count != Moves.Count)
		{
			return false;
		}
		if (!graph.Icons.SequenceEqual(Icons) || !graph.IconGroups.SequenceEqual(IconGroups) || !graph.Labels.SequenceEqual(Labels) || !graph.Arrows.SequenceEqual(Arrows) || !graph.Moves.SequenceEqual(Moves))
		{
			return false;
		}
		return true;
	}
}
