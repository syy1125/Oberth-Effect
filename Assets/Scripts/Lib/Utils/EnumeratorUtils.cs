using System;
using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Lib.DataStructures;

namespace Syy1125.OberthEffect.Lib.Utils
{
public static class EnumeratorUtils
{
	public static IEnumerable<T> MergeSorted<T, TKey>(this IEnumerable<IEnumerable<T>> source, Func<T, TKey> keyFn)
		where TKey : IComparable<TKey>
	{
		var heap = new MinHeap<IEnumerator<T>, TKey>(
			source
				.Select(item => item.GetEnumerator())
				.Where(e => e.MoveNext())
				.ToList(),
			e => keyFn(e.Current)
		);

		while (heap.Count > 0)
		{
			IEnumerator<T> current = heap.Pop();
			yield return current.Current;

			if (current.MoveNext())
			{
				heap.Add(current);
			}
			else
			{
				current.Dispose();
			}
		}
	}

	public static IEnumerable<T> MergeSorted<T>(this IEnumerable<IEnumerable<T>> source)
		where T : IComparable<T>
	{
		return MergeSorted(source, MinHeap<T>.Identity);
	}
}
}