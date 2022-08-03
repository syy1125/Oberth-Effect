using System;
using System.Collections.Generic;
using System.Text;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.CombatSystem;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Colors;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Foundation.Utils;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Block.Weapon;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;
using Syy1125.OberthEffect.Spec.Unity;
using Syy1125.OberthEffect.Spec.Validation;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.CoreMod.Weapons.Launcher
{
public enum AimPointScalingMode
{
	None,
	Lifetime,
	Velocity
}

[CreateSchemaFile("ProjectileLauncherSpecSchema")]
public class ProjectileLauncherSpec : AbstractWeaponLauncherSpec
{
	public Vector2 ColliderSize;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxSpeed;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxLifetime;

	public AimPointScalingMode AimPointScaling;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float AimPointScaleFactor = 1f;

	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public RendererSpec[] ProjectileRenderers;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public ParticleSystemSpec[] TrailParticles;

	[ValidateRangeInt(1, int.MaxValue)]
	public int BurstCount = 1;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float BurstInterval;
	// [ValidateRangeFloat(-180f, 180f)]
	public float[] ClusterBaseAngles = { 0f };

	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float SpreadAngle;
	public WeaponSpreadProfile SpreadProfile;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float Recoil;

	public PointDefenseTargetSpec PointDefenseTarget;
	[ValidateRangeFloat(0f, 1f)]
	public float HealthDamageScaling = 0.75f;

	public ScreenShakeSpec ScreenShake;

	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float ReloadTime;

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

public class ProjectileLauncher : AbstractWeaponLauncher
{
	private Camera _camera;
	private Rigidbody2D _body;
	private ColorContext _colorContext;

	private ProjectileConfig _projectileConfig;

	private int _burstCount;
	private float _burstInterval;
	private float[] _clusterBaseAngles;
	private float _spreadAngle;
	private WeaponSpreadProfile _spreadProfile;
	private float _maxSpeed;
	private float _maxLifetime;
	private AimPointScalingMode _aimPointScaling;
	private float _aimPointScaleFactor;
	private float _recoil;
	private float _reloadTime;

	private float _reloadProgress;

	private ScreenShakeSpec _screenShake;

	private void Awake()
	{
		_camera = Camera.main;
		_body = GetComponentInParent<Rigidbody2D>();
		_colorContext = GetComponentInParent<ColorContext>();
	}

	public void LoadSpec(ProjectileLauncherSpec spec, in BlockContext context)
	{
		base.LoadSpec(spec, context);

		_projectileConfig = spec.PointDefenseTarget == null
			? new()
			{
				ColliderSize = spec.ColliderSize,
				Damage = spec.Damage,
				DamageType = spec.DamageType,
				ArmorPierce = spec.ArmorPierce,
				ExplosionRadius = spec.ExplosionRadius,
				ColorScheme = _colorContext.ColorScheme,
				Renderers = spec.ProjectileRenderers,
				TrailParticles = spec.TrailParticles
			}
			: new NetworkedProjectileConfig
			{
				ColliderSize = spec.ColliderSize,
				Damage = spec.Damage,
				DamageType = spec.DamageType,
				ArmorPierce = spec.ArmorPierce,
				ExplosionRadius = spec.ExplosionRadius,
				ColorScheme = _colorContext.ColorScheme,
				Renderers = spec.ProjectileRenderers,
				TrailParticles = spec.TrailParticles,

				PointDefenseTarget = spec.PointDefenseTarget,
				HealthDamageScaling = spec.HealthDamageScaling,
			};

		_burstCount = spec.BurstCount;
		_burstInterval = spec.BurstInterval;
		_clusterBaseAngles = spec.ClusterBaseAngles;
		_spreadAngle = spec.SpreadAngle;
		_spreadProfile = spec.SpreadProfile;
		_maxSpeed = spec.MaxSpeed;
		_maxLifetime = spec.MaxLifetime;
		MaxRange = _maxSpeed * _maxLifetime;
		_aimPointScaling = spec.AimPointScaling;
		_aimPointScaleFactor = spec.AimPointScaleFactor;

		_recoil = spec.Recoil;
		_reloadTime = spec.ReloadTime;

		ReloadResourceUse = spec.MaxResourceUse;

		_screenShake = spec.ScreenShake;
	}

	private void Start()
	{
		_reloadProgress = _reloadTime;
	}

	public override IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest()
	{
		return _reloadProgress >= _reloadTime ? null : ReloadResourceUse;
	}

	public override void GetMaxFirepower(IList<FirepowerEntry> entries)
	{
		entries.Add(
			new FirepowerEntry
			{
				DamageType = _projectileConfig.DamageType,
				DamagePerSecond = _projectileConfig.Damage * _clusterBaseAngles.Length * _burstCount / _reloadTime,
				ArmorPierce = _projectileConfig.DamageType == DamageType.Explosive ? 1f : _projectileConfig.ArmorPierce
			}
		);
	}

	public override bool GetTooltip(StringBuilder builder, string indent)
	{
		builder
			.AppendLine($"{indent}Projectile")
			.AppendLine(
				_projectileConfig.DamageType == DamageType.Explosive
					? $"{indent}  {_projectileConfig.Damage:F0} {DamageTypeUtils.GetColoredText(_projectileConfig.DamageType)} damage, {PhysicsUnitUtils.FormatLength(_projectileConfig.ExplosionRadius)} radius"
					: $"{indent}  {_projectileConfig.Damage:F0} {DamageTypeUtils.GetColoredText(_projectileConfig.DamageType)} damage, <color=\"lightblue\">{_projectileConfig.ArmorPierce:0.#} AP</color>"
			)
			.AppendLine(
				$"{indent}  Max range {PhysicsUnitUtils.FormatSpeed(_maxSpeed)} × {_maxLifetime}s = {PhysicsUnitUtils.FormatDistance(MaxRange)}"
			);

		if (AimCorrection > Mathf.Epsilon)
		{
			builder.AppendLine($"{indent}  {AimCorrection}° aim correction");
		}

		if (_projectileConfig is NetworkedProjectileConfig { PointDefenseTarget: {} } networkedProjectileConfig)
		{
			builder.AppendLine(
				$"{indent}  Projectile has <color=\"red\">{networkedProjectileConfig.PointDefenseTarget.MaxHealth} health</color>, <color=\"lightblue\">{networkedProjectileConfig.PointDefenseTarget.ArmorValue} armor</color>"
			);

			if (!Mathf.Approximately(networkedProjectileConfig.HealthDamageScaling, 0f))
			{
				builder.AppendLine(
					$"{indent}  Damage reduced by up to {networkedProjectileConfig.HealthDamageScaling:00.#%}, scaling with fraction of health lost"
				);
			}
		}

		string reloadCost = string.Join(" ", VehicleResourceDatabase.Instance.FormatResourceDict(ReloadResourceUse));
		builder.AppendLine(
			ReloadResourceUse.Count > 0
				? $"{indent}  Reload time {_reloadTime}s, reload cost {reloadCost}/s"
				: $"{indent}  Reload time {_reloadTime}"
		);

		if (_clusterBaseAngles.Length > 1)
		{
			builder.AppendLine(
				_burstCount > 1
					? $"{indent}  {_clusterBaseAngles.Length} shots per cluster, {_burstCount} clusters per burst, {_burstInterval}s between clusters in burst"
					: $"{indent}  {_clusterBaseAngles.Length} shots per cluster"
			);
		}
		else if (_burstCount > 1)
		{
			builder.AppendLine($"{indent}  {_burstCount} shots per burst, {_burstInterval}s between shots in burst");
		}

		switch (_spreadProfile)
		{
			case WeaponSpreadProfile.None:
				break;
			case WeaponSpreadProfile.Gaussian:
				builder.AppendLine($"{indent}  Gaussian spread ±{_spreadAngle}°");
				break;
			case WeaponSpreadProfile.Uniform:
				builder.AppendLine($"{indent}  Uniform spread ±{_spreadAngle}°");
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		if (_recoil > Mathf.Epsilon)
		{
			string shotOrCluster = _clusterBaseAngles.Length > 1 ? "cluster" : "shot";
			builder.AppendLine(
				$"{indent}  Recoil {PhysicsUnitUtils.FormatImpulse(_recoil)} per {shotOrCluster}"
			);
		}

		return true;
	}

	public override Vector2? GetInterceptPoint(Vector2 relativePosition, Vector2 relativeVelocity)
	{
		bool success = InterceptSolver.ProjectileIntercept(
			relativePosition, relativeVelocity, _maxSpeed,
			out Vector2 interceptVelocity, out float hitTime
		);

		if (success)
		{
			// Because of how aim point calculation is done, we actually should omit our own velocity here.
			return interceptVelocity * hitTime;
		}

		// If weapon has AOE, see if closest approach is good enough
		if (_projectileConfig.DamageType == DamageType.Explosive)
		{
			float missMargin = Vector2.Distance(
				relativePosition + relativeVelocity * hitTime, interceptVelocity * hitTime
			);
			if (missMargin < _projectileConfig.ExplosionRadius)
			{
				return interceptVelocity * hitTime;
			}
		}

		return null;
	}

	public override void LauncherFixedUpdate(bool isMine, bool firing)
	{
		if (isMine)
		{
			if (firing && _reloadProgress >= _reloadTime)
			{
				_reloadProgress -= _reloadTime;

				FireCluster();

				for (int i = 1; i < _burstCount; i++)
				{
					Invoke(nameof(FireCluster), _burstInterval * i);
				}
			}

			if (_reloadProgress < _reloadTime)
			{
				_reloadProgress += Time.fixedDeltaTime * ResourceSatisfaction;
			}
		}
	}

	private void FireCluster()
	{
		var firingPort = transform;
		var position = firingPort.position;
		var rotation = firingPort.rotation;

		float aimPointScale = 1f;
		if (AimPoint != null && !Mathf.Approximately(MaxRange, 0f))
		{
			aimPointScale = Mathf.Min(
				Vector2.Distance(AimPoint.Value, position) * _aimPointScaleFactor / MaxRange, 1f
			);
		}

		float correctionAngle = GetCorrectionAngle();

		foreach (float baseAngle in _clusterBaseAngles)
		{
			float speed = _maxSpeed;
			_projectileConfig.Lifetime = _maxLifetime;

			switch (_aimPointScaling)
			{
				case AimPointScalingMode.None:
					break;
				case AimPointScalingMode.Lifetime:
					_projectileConfig.Lifetime = _maxLifetime * aimPointScale;
					break;
				case AimPointScalingMode.Velocity:
					speed = _maxSpeed * aimPointScale;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			float deviationAngle =
				(correctionAngle + baseAngle + WeaponSpreadUtils.GetDeviationAngle(_spreadProfile, _spreadAngle))
				* Mathf.Deg2Rad;
			Quaternion projectileRotation =
				Quaternion.AngleAxis(deviationAngle * Mathf.Rad2Deg, Vector3.back) * rotation;
			Vector2 projectileVelocity =
				_body.GetPointVelocity(position)
				+ (Vector2) firingPort.TransformVector(
					speed * Mathf.Sin(deviationAngle), speed * Mathf.Cos(deviationAngle), 0f
				);

			if (_projectileConfig is NetworkedProjectileConfig networkedProjectileConfig)
			{
				NetworkedProjectileManager.Instance.CreateProjectile(
					position, projectileRotation, projectileVelocity, networkedProjectileConfig
				);
			}
			else
			{
				SimpleProjectileManager.Instance.CreateProjectile(
					position, projectileRotation, projectileVelocity, _projectileConfig
				);
			}
		}

		if (_recoil > Mathf.Epsilon)
		{
			_body.AddForceAtPosition(-firingPort.up * _recoil, position, ForceMode2D.Impulse);
		}

		if (_screenShake != null)
		{
			_camera.GetComponentInParent<CameraScreenShake>()?.AddInstance(
				_screenShake.Strength, _screenShake.Duration, _screenShake.DecayCurve
			);
		}

		ExecuteWeaponSideEffects();
	}
}
}