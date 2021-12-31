using System;
using System.Collections.Generic;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.Unity;
using Syy1125.OberthEffect.Spec.Validation;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Block.Weapon
{
public enum AimPointScalingMode
{
	None,
	Lifetime,
	Velocity
}

public class ProjectileWeaponEffectSpec : AbstractWeaponEffectSpec
{
	[ValidateRangeInt(1, int.MaxValue)]
	public int ClusterCount = 1;
	[ValidateRangeInt(1, int.MaxValue)]
	public int BurstCount = 1;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float BurstInterval;

	public Vector2 ColliderSize;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxSpeed;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxLifetime;

	public AimPointScalingMode AimPointScaling;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float AimPointScaleFactor = 1f;

	public PointDefenseTargetSpec PointDefenseTarget;
	[ValidateRangeFloat(0f, 1f)]
	public float HealthDamageScaling = 0.8f;

	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float ReloadTime;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public RendererSpec[] Renderers;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public ParticleSystemSpec[] ProjectileParticles;

	public override void Validate(List<string> path, List<string> errors)
	{
		base.Validate(path, errors);
		
		if (BurstCount > 1 && BurstInterval * (BurstCount - 1) > ReloadTime)
		{
			errors.Add(
				ValidationHelper.FormatValidationError(path, "burst duration should not be longer than reload time")
			);
		}
	}
}
}