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

	/// <summary>
	/// Tell the target to take damage.
	/// <br/>
	/// Should only be called if we are responsible for calculating damage.
	/// </summary>
	void TakeDamage(DamageType damageType, ref float damage, float armorPierce, out bool damageExhausted);

	/// <summary>
	/// Requests that the target do beam damage calculations on the proper server.
	/// Typically this involves sending an RPC to the owner so that it can do the correct damage calculations.
	/// </summary>
	void RequestBeamDamage(DamageType damageType, float damage, float armorPierce, int ownerId, Vector2 beamStart, Vector2 beamEnd);
}
}