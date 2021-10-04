using System;
using System.Collections.Generic;
using UnityEngine;

namespace Syy1125.OberthEffect.Common.Utils
{
public static class ExplosionUtils
{
	public static Tuple<float, float> CombineExplosions(IEnumerable<Tuple<float, float>> explosions)
	{
		float radius = 0f, damage = 0f;

		foreach ((float explosionRadius, float explosionDamage) in explosions)
		{
			radius += Mathf.Pow(explosionRadius, 3f);
			damage += explosionDamage;
		}

		if (radius > Mathf.Epsilon && damage > Mathf.Epsilon)
		{
			return new Tuple<float, float>(Mathf.Pow(radius, 1f / 3f), damage);
		}
		else
		{
			return new Tuple<float, float>(0f, 0f);
		}
	}
}
}