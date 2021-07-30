﻿using Syy1125.OberthEffect.Spec.Block.Propulsion;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Block.Weapon
{
public class ProjectileWeaponEffectSpec : AbstractWeaponEffectSpec
{
	public int ClusterCount = 1;
	public int BurstCount = 1;

	public Vector2 ColliderSize;
	public float Speed;
	public float MaxLifetime;

	public float ReloadTime;
	public RendererSpec[] Renderers;
	public ParticleSystemSpec[] ProjectileParticles;
}
}