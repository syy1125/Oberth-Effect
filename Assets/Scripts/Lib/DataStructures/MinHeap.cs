using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Syy1125.OberthEffect.Common.Utils
{
public class MinHeap<T, TKey> : ICollection<T> where TKey : IComparable<TKey>
{
	protected const int DEFAULT_CAPACITY = 16;

	public int Count { get; private set; }
	public bool IsReadOnly => false;
	private T[] _values;
	private TKey[] _keys;
	private Func<T, TKey> _keyFn;

	public MinHeap(int capacity, Func<T, TKey> keyFn)
	{
		capacity = Mathf.NextPowerOfTwo(capacity);
		_values = new T[capacity];
		_keys = new TKey[capacity];
		_keyFn = keyFn;
	}

	public MinHeap(IList<T> content, Func<T, TKey> keyFn) : this(content.Count, keyFn)
	{
		for (int i = 0; i < content.Count; i++)
		{
			T item = content[i];
			_values[i] = item;
			_keys[i] = _keyFn(item);
		}

		Count = content.Count;

		for (int i = ParentOf(Count) - 1; i >= 0; i--)
		{
			PropagateDown(i);
		}
	}

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
			int capacity = _values.Length * 2;
			T[] newValues = new T[capacity];
			TKey[] newKeys = new TKey[capacity];

			_values.CopyTo(newValues, 0);
			_values = newValues;
			_keys.CopyTo(newKeys, 0);
			_keys = newKeys;
		}

		_values[Count] = item;
		_keys[Count] = _keyFn(item);
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
		return _keys[leftNode].CompareTo(_keys[rightNode]);
	}

	private void Swap(int leftNode, int rightNode)
	{
		(_values[leftNode], _values[rightNode]) = (_values[rightNode], _values[leftNode]);
		(_keys[leftNode], _keys[rightNode]) = (_keys[rightNode], _keys[leftNode]);
	}

	#endregion

	public override string ToString()
	{
		return $"MinHeap {{{string.Join(", ", _values.Take(Count))}}}";
	}
}

public class MinHeap<T> : MinHeap<T, T> where T : IComparable<T>
{
	public static T Identity(T item) => item;

	public MinHeap(int capacity, Func<T, T> keyFn) : base(capacity, keyFn)
	{}

	public MinHeap(int capacity = DEFAULT_CAPACITY) : this(capacity, Identity)
	{}

	public MinHeap(IList<T> content, Func<T, T> keyFn) : base(content, keyFn)
	{}

	public MinHeap(IList<T> content) : this(content, Identity)
	{}
}
}