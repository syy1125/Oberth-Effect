using Syy1125.OberthEffect.Spec.Block.Propulsion;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Block.Weapon
{
public class ProjectileWeaponEffectSpec : AbstractWeaponEffectSpec
{
	public int ClusterCount = 1;
	public int BurstCount = 1;
	public float Speed;
	public Vector2 Size;

	public float ReloadTime;
	public float MaxLifetime;
	public RendererSpec[] Renderers;
	public ParticleSystemSpec[] ProjectileParticles;
}
}