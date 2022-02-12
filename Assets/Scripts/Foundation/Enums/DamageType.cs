using System;
using UnityEngine;

namespace Syy1125.OberthEffect.Foundation.Enums
{
public enum DamageType
{
	Kinetic,
	Energy,
	Explosive,
}

public static class DamageTypeUtils
{
	private static readonly Color _silver;

	public static string GetColoredText(DamageType damageType)
	{
		return damageType switch
		{
			DamageType.Kinetic => "<color=\"silver\">Kinetic</color>",
			DamageType.Energy => "<color=\"lime\">Energy</color>",
			DamageType.Explosive => "<color=\"red\">Explosive</color>",
			_ => throw new ArgumentOutOfRangeException(nameof(damageType), damageType, null)
		};
	}
}
}