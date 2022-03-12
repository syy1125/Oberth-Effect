﻿using System;
using System.Collections.Generic;
using System.Text;
using Photon.Pun;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Colors;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Foundation.Utils;
using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Block.Weapon;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect.Emitter
{
public class ProjectileWeaponEffectEmitter : AbstractWeaponEffectEmitter
{
	private Camera _camera;
	private Rigidbody2D _body;
	private ColorContext _colorContext;

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
	private ProjectileConfig _projectileConfig;

	private float _reloadProgress;

	private ScreenShakeSpec _screenShake;

	private void Awake()
	{
		_camera = Camera.main;
		_body = GetComponentInParent<Rigidbody2D>();
		_colorContext = GetComponentInParent<ColorContext>();
	}

	public void LoadSpec(ProjectileWeaponEffectSpec spec)
	{
		base.LoadSpec(spec);

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

		_projectileConfig = new ProjectileConfig
		{
			ColliderSize = spec.ColliderSize,
			Damage = spec.Damage,
			DamageType = spec.DamageType,
			ArmorPierce = spec.ArmorPierce,
			ExplosionRadius = spec.ExplosionRadius,
			Renderers = spec.Renderers,
			TrailParticles = spec.TrailParticles
		};

		if (spec.PointDefenseTarget != null)
		{
			_projectileConfig.IsPointDefenseTarget = true;
			_projectileConfig.MaxHealth = spec.PointDefenseTarget.MaxHealth;
			_projectileConfig.ArmorValue = spec.PointDefenseTarget.ArmorValue;
			_projectileConfig.HealthDamageScaling = spec.HealthDamageScaling;
		}

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

	public override string GetEmitterTooltip()
	{
		StringBuilder builder = new StringBuilder();

		builder
			.AppendLine("  Projectile")
			.AppendLine(
				_projectileConfig.DamageType == DamageType.Explosive
					? $"    {_projectileConfig.Damage:F0} {DamageTypeUtils.GetColoredText(_projectileConfig.DamageType)} damage, {PhysicsUnitUtils.FormatLength(_projectileConfig.ExplosionRadius)} radius"
					: $"    {_projectileConfig.Damage:F0} {DamageTypeUtils.GetColoredText(_projectileConfig.DamageType)} damage, <color=\"lightblue\">{_projectileConfig.ArmorPierce:0.#} AP</color>"
			)
			.AppendLine(
				$"    Max range {PhysicsUnitUtils.FormatSpeed(_maxSpeed)} × {_maxLifetime}s = {PhysicsUnitUtils.FormatDistance(MaxRange)}"
			);

		if (_projectileConfig.IsPointDefenseTarget)
		{
			builder.AppendLine(
				$"    Projectile has <color=\"red\">{_projectileConfig.MaxHealth} health</color>, <color=\"lightblue\">{_projectileConfig.ArmorValue} armor</color>"
			);
		}

		string reloadCost = string.Join(" ", VehicleResourceDatabase.Instance.FormatResourceDict(ReloadResourceUse));
		builder.AppendLine(
			ReloadResourceUse.Count > 0
				? $"    Reload time {_reloadTime}s, reload cost {reloadCost}/s"
				: $"    Reload time {_reloadTime}"
		);

		if (_clusterBaseAngles.Length > 1)
		{
			builder.AppendLine(
				_burstCount > 1
					? $"    {_clusterBaseAngles.Length} shots per cluster, {_burstCount} clusters per burst, {_burstInterval}s between clusters in burst"
					: $"    {_clusterBaseAngles.Length} shots per cluster"
			);
		}
		else if (_burstCount > 1)
		{
			builder.AppendLine($"    {_burstCount} shots per burst, {_burstInterval}s between shots in burst");
		}

		switch (_spreadProfile)
		{
			case WeaponSpreadProfile.None:
				break;
			case WeaponSpreadProfile.Gaussian:
				builder.AppendLine($"    Gaussian spread ±{_spreadAngle}°");
				break;
			case WeaponSpreadProfile.Uniform:
				builder.AppendLine($"    Uniform spread ±{_spreadAngle}°");
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		if (_recoil > Mathf.Epsilon)
		{
			string shotOrCluster = _clusterBaseAngles.Length > 1 ? "cluster" : "shot";
			builder.AppendLine(
				$"    Recoil {PhysicsUnitUtils.FormatImpulse(_recoil)} per {shotOrCluster}"
			);
		}

		return builder.ToString();
	}

	public override Vector2 GetInterceptPoint(
		Vector2 ownPosition, Vector2 ownVelocity, Vector2 targetPosition, Vector2 targetVelocity
	)
	{
		Vector2 relativePosition = targetPosition - ownPosition;
		Vector2 relativeVelocity = targetVelocity - ownVelocity;

		InterceptSolver.ProjectileIntercept(
			relativePosition, relativeVelocity, _maxSpeed,
			out Vector2 interceptVelocity, out float hitTime
		);

		// Because of how aim point calculation is done, we actually should omit our own velocity here.
		return ownPosition + interceptVelocity * hitTime;
	}

	public override void EmitterFixedUpdate(bool isMine, bool firing)
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

			float deviationAngle = (baseAngle + WeaponSpreadUtils.GetDeviationAngle(_spreadProfile, _spreadAngle))
			                       * Mathf.Deg2Rad;
			GameObject projectile = PhotonNetwork.Instantiate(
				"Weapon Projectile", position, rotation,
				data: new object[]
				{
					CompressionUtils.Compress(JsonUtility.ToJson(_projectileConfig)),
					JsonUtility.ToJson(_colorContext.ColorScheme)
				}
			);

			var projectileBody = projectile.GetComponent<Rigidbody2D>();
			projectileBody.velocity =
				_body.GetPointVelocity(position)
				+ (Vector2) firingPort.TransformVector(Mathf.Sin(deviationAngle), Mathf.Cos(deviationAngle), 0f)
				* speed;
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