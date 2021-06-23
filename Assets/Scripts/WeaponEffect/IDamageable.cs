namespace Syy1125.OberthEffect.WeaponEffect
{
public interface IDamageable
{
	bool IsMine { get; }

	int OwnerId { get; }

	float Health { get; }

	float GetDamageModifier(float armorPierce, DamageType damageType);

	void TakeDamage(float damage);

	void DestroyByDamage();
}
}