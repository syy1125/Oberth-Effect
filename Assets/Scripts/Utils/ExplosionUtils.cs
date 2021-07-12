using UnityEngine;

namespace Syy1125.OberthEffect.Utils
{
public static class ExplosionUtils
{
	// TODO
	public static float GetExplosionRadius(float strength)
	{
		return 0f;
	}

	public static float GetExplosionDamage(float strength)
	{
		return 0f;
	}

	/// <summary>
	/// Estimate the fraction of a rectangle's area using a grid
	/// </summary>
	/// <param name="minPos">The minimum corner of the rectangle</param>
	/// <param name="maxPos">The maximum corner of the rectangle</param>
	/// <param name="center">The center of the circle</param>
	/// <param name="radius">The radius of the circle</param>
	/// <param name="gridResolution">How many grid points along each axis to use</param>
	/// <returns></returns>
	public static float EstimateOverlapFraction(
		Vector2 minPos, Vector2 maxPos, Vector2 center, float radius, int gridResolution = 100
	)
	{
		float sqrRadius = radius * radius;

		int attempts = gridResolution * gridResolution;
		float hits = 0f;

		for (float x = 0.5f; x < gridResolution; x++)
		{
			for (float y = 0.5f; y < gridResolution; y++)
			{
				Vector2 position = new Vector2(
					Mathf.Lerp(minPos.x, maxPos.x, x / gridResolution),
					Mathf.Lerp(minPos.y, maxPos.y, y / gridResolution)
				);
				if ((position - center).sqrMagnitude < sqrRadius) hits++;
			}
		}

		return hits / attempts;
	}
}
}