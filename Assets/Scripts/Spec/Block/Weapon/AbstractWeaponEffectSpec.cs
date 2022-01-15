using System.Collections.Generic;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.Unity;
using Syy1125.OberthEffect.Spec.Validation;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Block.Weapon
{
public abstract class AbstractWeaponEffectSpec : ICustomValidation
{
	public Vector2 FiringPortOffset;

	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float Damage;
	public DamageType DamageType;
	[ValidateRangeFloat(1f, 10f)]
	public float ArmorPierce = 1f;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float ExplosionRadius; // Only relevant for explosive damage

	public Dictionary<string, float> MaxResourceUse;

	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public ScreenShakeSpec ScreenShake;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public ParticleSystemSpec[] FireParticles;

	public virtual void Validate(List<string> path, List<string> errors)
	{
		ValidationHelper.ValidateFields(path, this, errors);
		path.Add(nameof(MaxResourceUse));
		ValidationHelper.ValidateResourceDictionary(path, MaxResourceUse, errors);
		path.RemoveAt(path.Count - 1);
	}
}
}