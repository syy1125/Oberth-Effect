using Syy1125.OberthEffect.Spec.Unity;
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
	public int ClusterCount = 1;
	public int BurstCount = 1;
	public float BurstInterval;

	public Vector2 ColliderSize;
	public float Speed;
	public float MaxLifetime;

	public AimPointScalingMode AimPointScaling;
	public float AimPointScaleFactor = 1f;

	public float ReloadTime;
	public RendererSpec[] Renderers;
	public ParticleSystemSpec[] ProjectileParticles;
}
}