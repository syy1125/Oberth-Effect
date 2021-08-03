using System;
using System.Collections.Generic;
using System.Text;
using Photon.Pun;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.ColorScheme;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Spec.Block.Weapon;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
public class ProjectileWeaponEffectEmitter : MonoBehaviour, IWeaponEffectEmitter
{
	private Rigidbody2D _body;
	private ColorContext _colorContext;

	private int _clusterCount;
	private int _burstCount;
	private float _burstInterval;

	private float _spreadAngle;
	private WeaponSpreadProfile _spreadProfile;
	private float _projectileSpeed;
	private float _recoil;

	private float _reloadTime;
	private BallisticProjectileConfig _projectileConfig;
	private Dictionary<string, float> _reloadResourceUse;

	private float _reloadProgress;
	private float _resourceSatisfaction;

	private void Awake()
	{
		_body = GetComponentInParent<Rigidbody2D>();
		_colorContext = GetComponentInParent<ColorContext>();
	}

	public void LoadSpec(ProjectileWeaponEffectSpec spec)
	{
		_clusterCount = spec.ClusterCount;
		_burstCount = spec.BurstCount;
		_burstInterval = spec.BurstInterval;

		_spreadAngle = spec.SpreadAngle;
		_spreadProfile = spec.SpreadProfile;
		_projectileSpeed = spec.Speed;
		_recoil = spec.Recoil;

		_reloadTime = spec.ReloadTime;

		_projectileConfig = new BallisticProjectileConfig
		{
			ColliderSize = spec.ColliderSize,
			Damage = spec.Damage,
			DamageType = spec.DamageType,
			ArmorPierce = spec.ArmorPierce,
			ExplosionRadius = spec.ExplosionRadius,
			Lifetime = spec.MaxLifetime,
			Renderers = spec.Renderers
		};

		_reloadResourceUse = spec.MaxResourceUse;
	}

	private void Start()
	{
		_reloadProgress = _reloadTime;
	}

	public IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest()
	{
		return _reloadProgress >= _reloadTime ? null : _reloadResourceUse;
	}

	public void SatisfyResourceRequestAtLevel(float level)
	{
		_resourceSatisfaction = level;
	}

	public IReadOnlyDictionary<DamageType, float> GetMaxFirepower()
	{
		return new Dictionary<DamageType, float>
		{
			{ _projectileConfig.DamageType, _projectileConfig.Damage * _clusterCount * _burstCount / _reloadTime }
		};
	}

	public string GetEmitterTooltip()
	{
		StringBuilder builder = new StringBuilder();
		builder
			.AppendLine("  Projectile")
			.AppendLine(
				_projectileConfig.DamageType == DamageType.Explosive
					? $"    {_projectileConfig.Damage:F0} {DamageTypeUtils.GetColoredText(_projectileConfig.DamageType)} damage, {_projectileConfig.ExplosionRadius * PhysicsConstants.METERS_PER_UNIT_LENGTH}m radius"
					: $"    {_projectileConfig.Damage:F0} {DamageTypeUtils.GetColoredText(_projectileConfig.DamageType)} damage, <color=\"lightblue\">{_projectileConfig.ArmorPierce:0.#} AP</color>"
			)
			.AppendLine($"    Speed {_projectileSpeed * PhysicsConstants.METERS_PER_UNIT_LENGTH:0.#}m/s")
			.AppendLine(
				$"    Max range {_projectileSpeed * _projectileConfig.Lifetime * PhysicsConstants.METERS_PER_UNIT_LENGTH:F0}m"
			);

		string reloadCost = string.Join(" ", VehicleResourceDatabase.Instance.FormatResourceDict(_reloadResourceUse));
		builder.AppendLine(
			_reloadResourceUse.Count > 0
				? $"    Reload time {_reloadTime}s, reload cost {reloadCost}"
				: $"    Reload time {_reloadTime}"
		);

		if (_clusterCount > 1)
		{
			builder.AppendLine(
				_burstCount > 1
					? $"    {_clusterCount} shots per cluster, {_burstCount} clusters per burst, {_burstInterval}s between clusters in burst"
					: $"    {_clusterCount} shots per cluster"
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
			string shotOrCluster = _clusterCount > 1 ? "cluster" : "shot";
			builder.AppendLine(
				$"    Recoil {_recoil * PhysicsConstants.KN_PER_UNIT_FORCE:#,0.#}kNs per {shotOrCluster}"
			);
		}

		return builder.ToString();
	}

	public void EmitterFixedUpdate(bool firing, bool isMine)
	{
		if (isMine)
		{
			if (firing && _reloadProgress >= _reloadTime)
			{
				_reloadProgress -= _reloadTime;

				FireCluster();

				for (int i = 1; i < _burstCount; i++)
				{
					FireCluster();
				}
			}

			if (_reloadProgress < _reloadTime)
			{
				_reloadProgress += Time.fixedDeltaTime * _resourceSatisfaction;
			}
		}
	}

	private void FireCluster()
	{
		var firingPort = transform;
		var position = firingPort.position;
		var rotation = firingPort.rotation;

		for (int i = 0; i < _clusterCount; i++)
		{
			float deviationAngle = WeaponSpreadUtils.GetDeviationAngle(_spreadProfile, _spreadAngle) * Mathf.Deg2Rad;
			GameObject projectile = PhotonNetwork.Instantiate(
				"Weapon Projectile", position, rotation,
				data: new object[]
				{
					JsonUtility.ToJson(_projectileConfig),
					JsonUtility.ToJson(_colorContext.ColorScheme)
				}
			);

			var projectileBody = projectile.GetComponent<Rigidbody2D>();
			projectileBody.velocity =
				_body.GetPointVelocity(position)
				+ (Vector2) firingPort.TransformVector(Mathf.Sin(deviationAngle), Mathf.Cos(deviationAngle), 0f)
				* _projectileSpeed;
		}

		if (_recoil > Mathf.Epsilon)
		{
			_body.AddForceAtPosition(-firingPort.up * _recoil, position, ForceMode2D.Impulse);
		}
	}
}
}