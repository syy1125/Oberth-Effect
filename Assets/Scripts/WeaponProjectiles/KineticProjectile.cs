using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Blocks.Weapons;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponProjectiles
{
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class KineticProjectile : MonoBehaviour, IWeaponProjectile
{
	public float Damage;
	[Range(1, 10)]
	public float ArmorPierce = 1;

	public IWeaponSystem From { get; set; }

	private void OnTriggerEnter2D(Collider2D other)
	{
		var block = other.GetComponentInParent<BlockCore>();
		if (block == null || block.OwnerId == From.GetOwnerId()) return;

		float damageModifier = block.GetDamageModifier(ArmorPierce, DamageType.Kinetic);
		float effectiveDamage = Damage * damageModifier;

		if (effectiveDamage > block.Health)
		{
			Damage -= block.Health / damageModifier;
			block.DestroyBlock();
		}
		else
		{
			block.DamageBlock(effectiveDamage);
			Destroy(gameObject);
		}
	}
}
}