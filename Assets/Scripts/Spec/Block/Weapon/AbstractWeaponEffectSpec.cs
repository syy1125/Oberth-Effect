using System.Collections.Generic;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Block.Weapon
{
public abstract class AbstractWeaponEffectSpec
{
	public Vector2 FiringPortOffset;

	public float Damage;
	public DamageType DamageType;
	public float ArmorPierce;
	public float ExplosionRadius; // Only relevant for explosive damage

	public float SpreadAngle;
	public WeaponSpreadProfile SpreadProfile;
	public float Recoil;

	public Dictionary<string, float> MaxResourceUse;

	public ParticleSystemSpec[] FireParticles;
}
}