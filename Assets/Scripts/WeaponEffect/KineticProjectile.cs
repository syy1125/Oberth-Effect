using Photon.Pun;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class KineticProjectile : MonoBehaviourPun
{
	public float Damage;
	[Range(1, 10)]
	public float ArmorPierce = 1;

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

		float damageModifier = target.GetDamageModifier(ArmorPierce, DamageType.Kinetic);
		float effectiveDamage = Damage * damageModifier;

		if (effectiveDamage > target.Health)
		{
			Debug.Log(
				$"Projectile {gameObject} hit {target} will deal {target.Health} damage and destroy block"
			);

			Damage -= target.Health / damageModifier;
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