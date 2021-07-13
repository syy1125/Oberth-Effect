using System;
using Photon.Pun;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
[Serializable]
public struct BallisticProjectileConfig
{
	public DamageType DamageType;
	public float Damage;
	[Range(1, 10)]
	public float ArmorPierce;
	public float Lifetime;
}

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BallisticProjectile : MonoBehaviourPun, IPunInstantiateMagicCallback
{
	private BallisticProjectileConfig _config;

	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		_config = JsonUtility.FromJson<BallisticProjectileConfig>((string) info.photonView.InstantiationData[0]);
	}

	private void Start()
	{
		Invoke(nameof(LifetimeDespawn), _config.Lifetime);
	}

	private static IDamageable GetDamageTarget(Transform target)
	{
		while (target != null)
		{
			foreach (MonoBehaviour behaviour in target.GetComponents<MonoBehaviour>())
			{
				if (behaviour is IDamageable damageable)
				{
					return damageable;
				}
			}

			target = target.parent;
		}

		return null;
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.isTrigger) return;

		IDamageable target = GetDamageTarget(other.transform);
		if (target == null || !target.IsMine || target.OwnerId == photonView.OwnerActorNr) return;

		// At this point, we've established that damage should be applied and that this client should be responsible for calculating damage.

		if (_config.DamageType == DamageType.Explosive)
		{
			// Explosive damage special case
			CreateExplosionAt(transform.position, _config.Damage);
		}
		else
		{
			// Normal projectile damage behaviour
			float damageModifier = target.GetDamageModifier(_config.ArmorPierce, _config.DamageType);
			float effectiveDamage = _config.Damage * damageModifier;

			if (effectiveDamage > target.Health)
			{
				Debug.Log(
					$"Projectile {gameObject} hit {target} will deal {target.Health} damage and destroy block"
				);

				_config.Damage -= target.Health / damageModifier;
				target.DestroyByDamage();
			}
			else
			{
				Debug.Log($"Projectile {gameObject} hit {target} will deal {effectiveDamage} damage");

				target.TakeDamage(effectiveDamage);
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
				photonView.RPC(nameof(CreateExplosionAt), RpcTarget.All, transform.position);
			}

			PhotonNetwork.Destroy(gameObject);
		}
	}

	[PunRPC]
	private void CreateExplosionAt(Vector3 position, float damage)
	{
		// TODO
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