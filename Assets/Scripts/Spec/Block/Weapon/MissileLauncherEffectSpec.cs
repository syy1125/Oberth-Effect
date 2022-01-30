using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.Unity;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Block.Weapon
{
public struct MissileLaunchTubeSpec
{
	public Vector2 Position;
	[ValidateRangeFloat(-360f, 360f)]
	public float Rotation;
	public Vector2 LaunchVelocity;
}

public class MissileLauncherEffectSpec : AbstractWeaponEffectSpec
{
	public float ProximityFuseRadius;
	public Vector2 ColliderSize;

	[ValidateNonNull]
	public MissileLaunchTubeSpec[] LaunchTubes;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float LaunchInterval;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxAcceleration;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxAngularAcceleration;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxLifetime;

	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float ThrustActivationDelay;
	public MissileGuidanceAlgorithm GuidanceAlgorithm;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float GuidanceActivationDelay;

	public PointDefenseTargetSpec PointDefenseTarget;
	public float HealthDamageScaling = 0.8f;

	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float ReloadTime;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public RendererSpec[] Renderers;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public ParticleSystemSpec[] PropulsionParticles;
}
}