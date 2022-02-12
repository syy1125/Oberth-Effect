using System;
using System.Collections.Generic;
using UnityEngine;

namespace Syy1125.OberthEffect.Foundation
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

	public BoundsInt ToBoundsInt()
	{
		return new BoundsInt(new Vector3Int(Min.x, Min.y, 0), new Vector3Int(Size.x, Size.y, 1));
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

	public BlockBounds Transformed(Vector2Int position, int rotation)
	{
		// There are some +1's in there to adjust for how BlockBounds is inclusive min and exclusive max
		switch (rotation % 4)
		{
			case 0:
				if (position == Vector2Int.zero) return this;
				return new BlockBounds(position + Min, position + Max);
			case 1:
				return new BlockBounds(
					new Vector2Int(position.x - Max.y + 1, position.y + Min.x),
					new Vector2Int(position.x - Min.y + 1, position.y + Max.x)
				);
			case 2:
				return new BlockBounds(
					position - Max + Vector2Int.one,
					position - Min + Vector2Int.one
				);
			case 3:
				return new BlockBounds(
					new Vector2Int(position.x + Min.y, position.y - Max.x + 1),
					new Vector2Int(position.x + Max.y, position.y - Min.x + 1)
				);
			default:
				throw new ArgumentException($"Invalid rotation {rotation}");
		}
	}
}
}