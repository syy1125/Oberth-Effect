using System;
using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Foundation.Enums;

namespace Syy1125.OberthEffect.CombatSystem
{
public struct FirepowerEntry
{
	public DamageType DamageType;
	public float DamagePerSecond;
	public float ArmorPierce;
}

public static class FirepowerUtils
{
	public static IList<FirepowerEntry> AggregateFirepower(IEnumerable<FirepowerEntry> entries)
	{
		var aggregator = new Dictionary<DamageType, Tuple<float, float>>();

		foreach (FirepowerEntry entry in entries)
		{
			if (!aggregator.TryGetValue(entry.DamageType, out var value))
			{
				value = new Tuple<float, float>(0f, 0f);
			}

			float damage = value.Item1 + entry.DamagePerSecond;
			float net = value.Item2 + entry.DamagePerSecond * entry.ArmorPierce;

			aggregator[entry.DamageType] = Tuple.Create(damage, net);
		}

		return aggregator
			.Select(
				pair => new FirepowerEntry
				{
					DamageType = pair.Key,
					DamagePerSecond = pair.Value.Item1,
					ArmorPierce = pair.Value.Item2 / pair.Value.Item1
				}
			).ToList();
	}

	public static float GetTotalDamage(IEnumerable<FirepowerEntry> entries)
	{
		return entries.Sum(entry => entry.DamagePerSecond);
	}

	public static float GetMeanArmorPierce(ICollection<FirepowerEntry> entries)
	{
		return entries.Sum(entry => entry.DamagePerSecond * entry.ArmorPierce)
		       / entries.Sum(entry => entry.DamagePerSecond);
	}
}
}