using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Syy1125.OberthEffect.Utils
{
public static class ExplosionUtils
{
	public static Tuple<float, float> CombineExplosions(IEnumerable<Tuple<float, float>> explosions)
	{
		float damage = 0f, radius = 0f;

		foreach ((float explosionDamage, float explosionRadius) in explosions)
		{
			damage += explosionDamage;
			radius += Mathf.Pow(explosionRadius, 3f);
		}

		if (damage > Mathf.Epsilon && radius > Mathf.Epsilon)
		{
			return new Tuple<float, float>(damage, Mathf.Pow(radius, 1f / 3f));
		}
		else
		{
			return new Tuple<float, float>(0f, 0f);
		}
	}
}
}