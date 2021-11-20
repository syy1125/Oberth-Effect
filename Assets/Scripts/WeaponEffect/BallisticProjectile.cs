using System;
using Photon.Pun;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Common.Utils;
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
	public float ArmorPierce; // Note that explosive damage will always have damage output value of 1
	public float ExplosionRadius; // Only relevant for explosive damage
	public float Lifetime;

	public RendererSpec[] Renderers;
}

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BallisticProjectile : MonoBehaviourPun, IPunInstantiateMagicCallback
{
	public BoxCollider2D ProjectileCollider;

	private BallisticProjectileConfig _config;
	private bool _expectExploded;

	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		_config = JsonUtility.FromJson<BallisticProjectileConfig>((string) info.photonView.InstantiationData[0]);
		_config.ArmorPierce = Mathf.Clamp(_config.ArmorPierce, 1, 10);

		ProjectileCollider.size = _config.ColliderSize;

		RendererHelper.AttachRenderers(transform, _config.Renderers);
	}

	private void Start()
	{
		Invoke(nameof(LifetimeDespawn), _config.Lifetime);
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.isTrigger) return;

		IDamageable target = other.transform.GetComponentInParent<IDamageable>();
		if (target == null || target.OwnerId == photonView.OwnerActorNr) return;

		if (!target.IsMine)
		{
			if (_config.DamageType == DamageType.Explosive)
			{
				gameObject.SetActive(false);
				_expectExploded = true;
			}

			return;
		}

		// At this point, we've established that damage should be applied and that this client should be responsible for calculating damage.

		if (_config.DamageType == DamageType.Explosive)
		{
			// Explosive damage special case
			photonView.RPC(
				nameof(CreateExplosionAt), RpcTarget.All,
				transform.position, _config.ExplosionRadius, _config.Damage
			);
			photonView.RPC(nameof(DestroyProjectile), photonView.Owner);
		}
		else
		{
			// Normal projectile damage behaviour
			target.TakeDamage(_config.DamageType, ref _config.Damage, _config.ArmorPierce, out bool damageExhausted);

			// I think the cases where the projectile hits multiple targets in a row are rare enough that we can neglect race conditions for now.
			photonView.RPC(nameof(SetRemainingDamage), RpcTarget.Others, _config.Damage);

			if (damageExhausted)
			{
				gameObject.SetActive(false);
				photonView.RPC(nameof(DestroyProjectile), photonView.Owner);
			}
		}
	}

	private void LifetimeDespawn()
	{
		gameObject.SetActive(false);

		if (photonView.IsMine)
		{
			if (_config.DamageType == DamageType.Explosive && !_expectExploded)
			{
				photonView.RPC(
					nameof(CreateExplosionAt), RpcTarget.All,
					transform.position, _config.ExplosionRadius, _config.Damage
				);
			}

			PhotonNetwork.Destroy(gameObject);
		}
	}

	[PunRPC]
	private void SetRemainingDamage(float damage)
	{
		_config.Damage = damage;
	}

	[PunRPC]
	private void CreateExplosionAt(Vector3 position, float radius, float damage)
	{
		ExplosionManager.Instance.CreateExplosionAt(position, radius, damage, photonView.OwnerActorNr);
	}

	[PunRPC]
	private void DestroyProjectile()
	{
		if (photonView.IsMine)
		{
			PhotonNetwork.Destroy(gameObject);
		}
	}
}
}