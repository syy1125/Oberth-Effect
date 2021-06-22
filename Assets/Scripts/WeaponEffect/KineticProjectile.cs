using Photon.Pun;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Blocks.Weapons;
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

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (!other.GetComponentInParent<PhotonView>().IsMine) return;

		var block = other.GetComponentInParent<BlockCore>();
		if (block == null || block.OwnerId == _ownerId) return;

		float damageModifier = block.GetDamageModifier(ArmorPierce, DamageType.Kinetic);
		float effectiveDamage = Damage * damageModifier;

		if (effectiveDamage > block.Health)
		{
			Debug.Log($"Projectile hit {block} will deal {block.Health} damage and destroy block");

			Damage -= block.Health / damageModifier;
			block.DestroyBlock();
		}
		else
		{
			Debug.Log($"Projectile hit {block} will deal {effectiveDamage} damage");

			block.DamageBlock(effectiveDamage);
			PhotonNetwork.Destroy(gameObject);
		}
	}
}
}