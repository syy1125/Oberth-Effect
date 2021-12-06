using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Common.Utils;
using UnityEngine;

namespace Syy1125.OberthEffect.Prototyping
{
public class Playground : MonoBehaviour
{
	private static int[] RandArray()
	{
		return new[]
			{
				Random.Range(1, 10),
				Random.Range(1, 10),
				Random.Range(1, 10),
				Random.Range(1, 10),
				Random.Range(1, 10),
				Random.Range(1, 10),
				Random.Range(1, 10),
				Random.Range(1, 10),
			}
			.OrderBy(item => item)
			.ToArray();
	}

	private void Start()
	{
		int counter = 0;

		IEnumerable<int> merged = new List<int[]>
		{
			RandArray(),
			RandArray(),
			RandArray(),
			RandArray()
		}.MergeSorted(
			Comparer<int>.Create(
				(left, right) =>
				{
					counter++;
					return left - right;
				}
			)
		);

		foreach (int i in merged)
		{
			Debug.Log(i);
		}

		Debug.Log($"Comparison count {counter}");
	}
}
}