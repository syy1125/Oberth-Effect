using System.Collections.Generic;
using Syy1125.OberthEffect.Common.Utils;
using UnityEngine;

namespace Syy1125.OberthEffect.Prototyping
{
public class Playground : MonoBehaviour
{
	private void Start()
	{
		MinHeap<int> heap = new MinHeap<int>(new List<int> { 4, 3, 3, 1, 2, 1, 4, 3 });

		while (heap.Count > 0)
		{
			Debug.Log(heap.Pop());
		}
	}
}
}