using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Syy1125.OberthEffect.Common.Utils
{
public class MinHeap<T> : ICollection<T>
{
	private const int DEFAULT_CAPACITY = 16;

	public int Count { get; private set; }
	public bool IsReadOnly => false;
	private T[] _values;
	private IComparer<T> _comparer;

	public MinHeap(int capacity, IComparer<T> comparer)
	{
		_values = new T[Mathf.NextPowerOfTwo(capacity)];
		_comparer = comparer;
	}

	public MinHeap(int capacity, Comparison<T> comparison) : this(capacity, Comparer<T>.Create(comparison))
	{}

	public MinHeap(int capacity = DEFAULT_CAPACITY) : this(capacity, Comparer<T>.Default)
	{}

	public MinHeap(IList<T> content, IComparer<T> comparer) : this(content.Count, comparer)
	{
		for (int i = 0; i < content.Count; i++)
		{
			_values[i] = content[i];
		}

		Count = content.Count;

		for (int i = ParentOf(Count) - 1; i >= 0; i--)
		{
			PropagateDown(i);
		}
	}

	public MinHeap(IList<T> content) : this(content, Comparer<T>.Default)
	{}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public IEnumerator<T> GetEnumerator()
	{
		return _values.Take(Count).GetEnumerator();
	}

	public void Add(T item)
	{
		if (Count >= _values.Length)
		{
			T[] newValues = new T[_values.Length * 2];
			_values.CopyTo(newValues, 0);
			_values = newValues;
		}

		_values[Count] = item;
		PropagateUp(Count);
		Count++;
	}

	public void Clear()
	{
		Count = 0;
	}

	public bool Contains(T item)
	{
		return _values.Take(Count).Contains(item);
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		_values.Take(Count).ToList().CopyTo(array, arrayIndex);
	}

	public bool Remove(T item)
	{
		for (int i = 0; i < Count; i++)
		{
			if (Equals(_values[i], item))
			{
				Swap(i, Count - 1);
				Count--;

				if (i > 0 && Compare(i, ParentOf(i)) < 0)
				{
					PropagateUp(i);
				}
				else
				{
					PropagateDown(i);
				}

				return true;
			}
		}

		return false;
	}

	public T Peek()
	{
		if (Count == 0) throw new IndexOutOfRangeException("MinHeap is empty");
		return _values[0];
	}

	public T Pop()
	{
		if (Count == 0) throw new IndexOutOfRangeException("MinHeap is empty");
		T value = _values[0];
		Swap(0, Count - 1);
		Count--;
		PropagateDown(0);
		return value;
	}

	#region Propagation

	private void PropagateDown(int node)
	{
		int left = LeftChildOf(node), right = RightChildOf(node);

		if (!InBounds(left)) return; // Leaf node

		if (InBounds(right))
		{
			// Two children
			if (Compare(node, left) > 0)
			{
				int child = Compare(left, right) < 0 ? left : right;
				Swap(node, child);
				PropagateDown(child);
			}
			else if (Compare(node, right) > 0)
			{
				Swap(node, right);
				PropagateDown(right);
			}
		}
		else
		{
			// One child
			if (Compare(node, left) > 0)
			{
				Swap(node, left);
				PropagateDown(left);
			}
		}
	}

	private void PropagateUp(int node)
	{
		while (node > 0)
		{
			int parent = ParentOf(node);
			if (Compare(node, parent) >= 0) return;
			Swap(node, parent);
			node = parent;
		}
	}

	#endregion

	#region Utility

	private static int ParentOf(int node)
	{
		return (node - 1) / 2;
	}

	private static int LeftChildOf(int node)
	{
		return node * 2 + 1;
	}

	private static int RightChildOf(int node)
	{
		return node * 2 + 2;
	}

	private bool InBounds(int node)
	{
		return node < Count;
	}

	private int Compare(int leftNode, int rightNode)
	{
		return _comparer.Compare(_values[leftNode], _values[rightNode]);
	}

	private void Swap(int leftNode, int rightNode)
	{
		(_values[leftNode], _values[rightNode]) = (_values[rightNode], _values[leftNode]);
	}

	#endregion

	public override string ToString()
	{
		return $"MinHeap {{{string.Join(", ", _values.Take(Count))}}}";
	}
}
}