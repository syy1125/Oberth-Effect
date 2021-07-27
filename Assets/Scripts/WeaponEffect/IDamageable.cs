using System;
using Syy1125.OberthEffect.Common.Enums;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
public interface IDamageable
{
	Transform transform { get; }

	bool IsMine { get; }

	int OwnerId { get; }

	Tuple<Vector2, Vector2> GetExplosionDamageBounds();

	void TakeDamage(DamageType damageType, ref float damage, float armorPierce, out bool damageExhausted);
}
}