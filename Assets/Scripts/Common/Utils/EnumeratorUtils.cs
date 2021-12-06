using System;
using System.Collections.Generic;
using System.Linq;

namespace Syy1125.OberthEffect.Common.Utils
{
public static class EnumeratorUtils
{
	public static IEnumerable<T> MergeSorted<T>(
		this IEnumerable<IEnumerable<T>> source, IComparer<T> comparer = null
	)
	{
		comparer ??= Comparer<T>.Default;

		var heap = new MinHeap<IEnumerator<T>>(
			source
				.Select(item => item.GetEnumerator())
				.Where(e => e.MoveNext())
				.ToList(),
			Comparer<IEnumerator<T>>.Create((left, right) => comparer.Compare(left.Current, right.Current))
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

	public static IEnumerable<T> MergeSorted<T>(this IEnumerable<IEnumerable<T>> source, Comparison<T> comparison)
	{
		return MergeSorted(source, Comparer<T>.Create(comparison));
	}
}
}