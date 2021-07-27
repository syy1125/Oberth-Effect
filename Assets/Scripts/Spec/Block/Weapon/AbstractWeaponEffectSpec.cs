using System.Collections.Generic;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Spec.Block.Propulsion;

namespace Syy1125.OberthEffect.Spec.Block.Weapon
{
public abstract class AbstractWeaponEffectSpec
{
	public Dictionary<DamageType, float> Damage;
	public float ArmorPierce;
	public float ExplosionRadius; // Only relevant for explosive damage

	public float SpreadAngle;
	public WeaponSpreadProfile SpreadProfile;
	public float Recoil;

	public Dictionary<string, float> ReloadResourceUse;

	public ParticleSystemSpec[] FireParticles;
}
}