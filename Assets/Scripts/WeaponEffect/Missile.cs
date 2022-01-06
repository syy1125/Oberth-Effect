using System;
using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Common.Physics;
using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
public struct MissileConfig
{
	public Vector2 ColliderSize;
	public float Damage;
	public DamageType DamageType;
	public float ArmorPierce;
	public float ExplosionRadius;
	public float Lifetime;

	public int TargetPhotonId;
	public float MaxAcceleration;
	public float MaxAngularAcceleration;
	public MissileGuidanceAlgorithm GuidanceAlgorithm;
	public float GuidanceActivationDelay;

	public bool IsPointDefenseTarget;
	public float MaxHealth;
	public float ArmorValue;
	public float HealthDamageScaling;

	public RendererSpec[] Renderers;
}

// Represents either a missile or a drone. (Or both)
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(DamagingProjectile))]
public class Missile : MonoBehaviourPun, IPunInstantiateMagicCallback
{
	private MissileConfig _config;
	private PointDefenseTarget _pdTarget;

	private float _initTime;

	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		object[] instantiationData = info.photonView.InstantiationData;
		_config = JsonUtility.FromJson<MissileConfig>(CompressionUtils.Decompress((byte[]) instantiationData[0]));

		GetComponent<DamagingProjectile>().Init(
			_config.Damage, _config.DamageType, _config.ArmorPierce, _config.ExplosionRadius, null
		);

		GetComponent<BoxCollider2D>().size = _config.ColliderSize;

		if (_config.IsPointDefenseTarget)
		{
			_pdTarget = gameObject.AddComponent<PointDefenseTarget>();
			_pdTarget.Init(_config.MaxHealth, _config.ArmorValue, _config.ColliderSize);
			// _pdTarget.OnDestroyedByDamage.AddListener(EndOfLifeDespawn);

			gameObject.AddComponent<ReferenceFrameProvider>();
			var radiusProvider = gameObject.AddComponent<ConstantCollisionRadiusProvider>();
			radiusProvider.Radius = _config.ColliderSize.magnitude / 2;
		}

		GetComponent<ConstantCollisionRadiusProvider>().Radius = _config.ColliderSize.magnitude / 2;

		RendererHelper.AttachRenderers(transform, _config.Renderers);

		_initTime = Time.time;
	}
}
}