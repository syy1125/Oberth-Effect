using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
public interface IDamageable
{
	Transform transform { get; }

	bool IsMine { get; }

	int OwnerId { get; }

	float Health { get; }

	Bounds GetExplosionDamageBounds();

	float GetDamageModifier(float armorPierce, DamageType damageType);

	void TakeDamage(float damage);

	void DestroyByDamage();
}
}