using System;
using Photon.Pun;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Common.Physics;
using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
[Serializable]
public struct BallisticProjectileConfig
{
	public Vector2 ColliderSize;
	public float Damage;
	public DamageType DamageType;
	public float ArmorPierce; // Note that explosive damage will always have armor pierce of 1
	public float ExplosionRadius; // Only relevant for explosive damage
	public float Lifetime;

	public bool IsPointDefenseTarget;
	public float MaxHealth;
	public float ArmorValue;
	public float HealthDamageScaling;

	public RendererSpec[] Renderers;
}

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(DamagingProjectile))]
public class BallisticProjectile : MonoBehaviourPun, IPunInstantiateMagicCallback
{
	private BallisticProjectileConfig _config;
	private PointDefenseTarget _pdTarget;

	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		object[] instantiationData = info.photonView.InstantiationData;
		_config = JsonUtility.FromJson<BallisticProjectileConfig>(
			CompressionUtils.Decompress((byte[]) instantiationData[0])
		);

		GetComponent<DamagingProjectile>().Init(
			_config.Damage, _config.DamageType, _config.ArmorPierce, _config.ExplosionRadius,
			GetHealthDamageModifier
		);

		GetComponent<BoxCollider2D>().size = _config.ColliderSize;

		if (_config.IsPointDefenseTarget)
		{
			_pdTarget = gameObject.AddComponent<PointDefenseTarget>();
			_pdTarget.Init(_config.MaxHealth, _config.ArmorValue, _config.ColliderSize);
			_pdTarget.OnDestroyedByDamage.AddListener(EndOfLifeDespawn);

			gameObject.AddComponent<ReferenceFrameProvider>();
			var radiusProvider = gameObject.AddComponent<ConstantCollisionRadiusProvider>();
			radiusProvider.Radius = _config.ColliderSize.magnitude / 2;
		}

		RendererHelper.AttachRenderers(transform, _config.Renderers);
	}

	private void Start()
	{
		Invoke(nameof(EndOfLifeDespawn), _config.Lifetime);
	}

	private float GetHealthDamageModifier()
	{
		return _pdTarget == null
			? 1f
			: MathUtils.Remap(
				_pdTarget.HealthFraction,
				0f, 1f, 1f - _config.HealthDamageScaling, 1f
			);
	}

	private void EndOfLifeDespawn()
	{
		GetComponent<DamagingProjectile>().OnLifetimeDespawn();
	}
}
}