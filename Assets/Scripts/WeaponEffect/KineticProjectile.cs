using Photon.Pun;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class KineticProjectile : MonoBehaviour, IPunInstantiateMagicCallback
{
	public float Damage;
	[Range(1, 10)]
	public float ArmorPierce = 1;

	private int _ownerId;

	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		_ownerId = info.Sender.ActorNumber;
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
		if (target == null || !target.IsMine || target.OwnerId == _ownerId) return;

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
			PhotonNetwork.Destroy(gameObject);
		}
	}
}
}