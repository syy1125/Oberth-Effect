using Syy1125.OberthEffect.Spec.Block.Propulsion;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Block.Weapon
{
public class ProjectileWeaponEffectSpec : AbstractWeaponEffectSpec
{
	public Vector2 ProjectileSize;
	public float MaxLifetime;
	public ParticleSystemSpec[] ProjectileParticles;
}
}