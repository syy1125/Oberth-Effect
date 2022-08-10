using System;
using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Foundation.Enums;

namespace Syy1125.OberthEffect.CombatSystem
{
public struct FirepowerEntry
{
	public string DamageTypeId;
	public float DamagePerSecond;
	public float ArmorPierce;
}

public static class FirepowerUtils
{
	public static IList<FirepowerEntry> AggregateFirepower(IEnumerable<FirepowerEntry> entries)
	{
		var aggregator = new Dictionary<string, Tuple<float, float>>();

		foreach (FirepowerEntry entry in entries)
		{
			if (!aggregator.TryGetValue(entry.DamageTypeId, out var value))
			{
				value = new(0f, 0f);
			}

			float damage = value.Item1 + entry.DamagePerSecond;
			float net = value.Item2 + entry.DamagePerSecond * entry.ArmorPierce;

			aggregator[entry.DamageTypeId] = Tuple.Create(damage, net);
		}

		return aggregator
			.Select(
				pair => new FirepowerEntry
				{
					DamageTypeId = pair.Key,
					DamagePerSecond = pair.Value.Item1,
					ArmorPierce = pair.Value.Item2 / pair.Value.Item1
				}
			)
			.OrderBy(entry => entry.DamageTypeId)
			.ToList();
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