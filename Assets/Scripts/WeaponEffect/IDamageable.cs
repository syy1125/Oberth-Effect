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

	int GetExplosionGridResolution();

	/// <summary>
	/// Creates a predicate for determining whether a local point is within the proper collider of the object.
	/// If null, this is interpreted as all points are always in bounds.
	/// </summary>
	/// <remarks>
	/// To future-proof this, make this function work well outside Unity's main thread. Don't call on any of unity's component system functions inside this function.
	/// </remarks>
	Predicate<Vector2> GetPointInBoundPredicate();

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
	void RequestBeamDamage(
		DamageType damageType, float damage, float armorPierce, int ownerId, Vector2 beamStart, Vector2 beamEnd
	);
}
}