using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Common.ColorScheme;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Spec.Block.Weapon;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
public class ProjectileWeaponEffectEmitter : MonoBehaviour, IWeaponEffectEmitter
{
	private Rigidbody2D _body;
	private ColorContext _colorContext;

	private int _clusterCount;
	private int _burstCount;
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