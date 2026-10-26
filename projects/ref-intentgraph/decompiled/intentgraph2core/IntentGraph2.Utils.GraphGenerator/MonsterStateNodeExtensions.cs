using System.Collections.Generic;

namespace IntentGraph2.Utils.GraphGenerator;

public static class MonsterStateNodeExtensions
{
	internal static HashSet<MonsterStateNode> GetAllNodes(this IEnumerable<MonsterStateNode> nodes)
	{
		HashSet<MonsterStateNode> hashSet = new HashSet<MonsterStateNode>();
		foreach (MonsterStateNode node in nodes)
		{
			foreach (MonsterStateNode allNode in node.GetAllNodes())
			{
				hashSet.Add(allNode);
			}
		}
		return hashSet;
	}
}
