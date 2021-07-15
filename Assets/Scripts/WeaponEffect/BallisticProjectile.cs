using System;
using Photon.Pun;
using Syy1125.OberthEffect.Utils;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
[Serializable]
public struct BallisticProjectileConfig
{
	public Vector2 ProjectileSize;
	public DamageType DamageType;
	public float Damage;
	[Range(1, 10)]
	public float ArmorPierce; // Note that explosive damage will always have damage output value of 1
	public float ExplosionRadius; // Only relevant for explosive damage
	public float Lifetime;
}

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BallisticProjectile : MonoBehaviourPun, IPunInstantiateMagicCallback
{
	public Transform ProjectileVisual;
	public BoxCollider2D ProjectileCollider; 
		
	private BallisticProjectileConfig _config;

	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		_config = JsonUtility.FromJson<BallisticProjectileConfig>((string) info.photonView.InstantiationData[0]);
		_config.ArmorPierce = Mathf.Clamp(_config.ArmorPierce, 1, 10);
		ProjectileVisual.localScale = new Vector3(_config.ProjectileSize.x, _config.ProjectileSize.y, 1f);
		ProjectileCollider.size = _config.ProjectileSize;
	}

	private void Start()
	{
		Invoke(nameof(LifetimeDespawn), _config.Lifetime);
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.isTrigger) return;

		IDamageable target = ComponentUtils.GetBehaviourInParent<IDamageable>(other.transform);
		if (target == null || !target.IsMine || target.OwnerId == photonView.OwnerActorNr) return;

		// At this point, we've established that damage should be applied and that this client should be responsible for calculating damage.

		if (_config.DamageType == DamageType.Explosive)
		{
			// Explosive damage special case
			photonView.RPC(
				nameof(CreateExplosionAt), RpcTarget.All,
				transform.position, _config.ExplosionRadius, _config.Damage
			);
		}
		else
		{
			// Normal projectile damage behaviour
			target.TakeDamage(_config.DamageType, ref _config.Damage, _config.ArmorPierce, out bool damageExhausted);

			if (damageExhausted)
			{
				gameObject.SetActive(false);
				photonView.RPC("DestroyProjectile", RpcTarget.All);
			}
		}
	}

	private void LifetimeDespawn()
	{
		gameObject.SetActive(false);

		if (photonView.IsMine)
		{
			if (_config.DamageType == DamageType.Explosive)
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
	private void CreateExplosionAt(Vector3 position, float radius, float damage)
	{
		ExplosionManager.Instance.CreateExplosionAt(position, radius, damage);
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