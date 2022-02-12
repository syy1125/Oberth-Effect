using System.Collections.Generic;
using UnityEngine;

namespace Syy1125.OberthEffect.Foundation
{
public static class RaycastHitComparer
{
	public static Comparer<RaycastHit2D> Default = Comparer<RaycastHit2D>.Create(DefaultComparison);

	public static Comparer<RaycastHit2D> Distance = Comparer<RaycastHit2D>.Create(DistanceComparison);

	private static int DefaultComparison(RaycastHit2D left, RaycastHit2D right)
	{
		return left.CompareTo(right);
	}

	private static int DistanceComparison(RaycastHit2D left, RaycastHit2D right)
	{
		if (left.collider is null)
		{
			return 1;
		}

		if (right.collider is null)
		{
			return -1;
		}

		return left.distance.CompareTo(right.distance);
	}
}
}