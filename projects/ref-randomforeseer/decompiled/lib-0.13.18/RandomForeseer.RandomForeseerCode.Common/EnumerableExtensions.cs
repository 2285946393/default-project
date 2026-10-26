using System.Collections.Generic;

namespace RandomForeseer.RandomForeseerCode.Common;

internal static class EnumerableExtensions
{
	public static IReadOnlyList<T> Materialize<T>(this IEnumerable<T> source)
	{
		object obj = source as IReadOnlyList<T>;
		if (obj == null)
		{
			List<T> list = new List<T>();
			list.AddRange(source);
			obj = new _003C_003Ez__ReadOnlyList<T>(list);
		}
		return (IReadOnlyList<T>)obj;
	}
}
