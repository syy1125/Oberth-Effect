using System;
using Syy1125.OberthEffect.Lib.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Syy1125.OberthEffect.CoreMod.Weapons
{
public enum WeaponSpreadProfile
{
	None,
	Gaussian,
	Uniform
}

public static class WeaponSpreadUtils
{
	public static float GetDeviationAngle(WeaponSpreadProfile spreadProfile, float spreadAngle)
	{
		return spreadProfile switch
		{
			WeaponSpreadProfile.None => 0f,
			WeaponSpreadProfile.Gaussian => MathUtils.RandomGaussian() * spreadAngle,
			WeaponSpreadProfile.Uniform => Random.Range(-spreadAngle, spreadAngle),
			_ => throw new ArgumentOutOfRangeException(nameof(spreadProfile), spreadProfile, null)
		};
	}

	public static float GetDeviationAngle(WeaponSpreadProfile spreadProfile, float spreadAngle, float seed, float time)
	{
		float randomValue = Mathf.PerlinNoise(seed, time);

		return spreadProfile switch
		{
			WeaponSpreadProfile.None => 0f,
			WeaponSpreadProfile.Gaussian => MathUtils.InverseNormal(
				                                // Avoid argument range issues and potential extreme values
				                                MathUtils.Remap(randomValue, 0f, 1f, 0.01f, 0.99f)
			                                )
			                                * spreadAngle,
			WeaponSpreadProfile.Uniform => MathUtils.Remap(randomValue, 0f, 1f, -spreadAngle, spreadAngle),
			_ => throw new ArgumentOutOfRangeException(nameof(spreadProfile), spreadProfile, null)
		};
	}
}
}