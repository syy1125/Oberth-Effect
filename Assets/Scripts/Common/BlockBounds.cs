using System;
using System.Collections.Generic;
using UnityEngine;

namespace Syy1125.OberthEffect.Common
{
public struct BlockBounds
{
	public readonly Vector2Int Min;
	public readonly Vector2Int Max;

	public readonly Vector2 Center;
	public readonly Vector2Int Size;

	public BlockBounds(Vector2Int min, Vector2Int max)
	{
		if (min.x >= max.x) throw new ArgumentException("BlockBounds Min.x >= Max.x");
		if (min.y >= max.y) throw new ArgumentException("BlockBounds Min.y >= Max.y");

		Min = min;
		Max = max;

		Center = (Vector2) (min + max) / 2;
		Size = max - min;
	}

	public IEnumerable<Vector2Int> AllPositionsWithin
	{
		get
		{
			for (int y = Min.y; y < Max.y; y++)
			{
				for (int x = Min.x; x < Max.x; x++)
				{
					yield return new Vector2Int(x, y);
				}
			}
		}
	}

	public static IEnumerable<Vector2Int> EnumeratePositionsWithin(Vector2Int min, Vector2Int max)
	{
		return new BlockBounds(min, max).AllPositionsWithin;
	}
}
}