using System.Collections.Generic;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Enums;
using UnityEngine;

namespace Syy1125.OberthEffect.CombatSystem
{
public static class BeamWeaponUtils
{
	private static readonly List<RaycastHit2D> hits = new();

	public static void HandleBeamDamage(
		DamageType damageType, float damage, float armorPierce, int ownerId,
		Vector2 beamStart, Vector2 beamEnd
	)
	{
		Vector2 beamDirection = beamEnd - beamStart;
		int count = Physics2D.Raycast(
			beamStart, beamDirection, LayerConstants.WeaponHitFilter, hits, beamDirection.magnitude
		);

		if (count > 0)
		{
			hits.Sort(0, count, RaycastHitComparer.Default);

			for (int i = 0; i < count; i++)
			{
				RaycastHit2D hit = hits[i];
				IDamageable target = hit.collider.GetComponentInParent<IDamageable>();

				// Only do damage calculations for blocks we own
				if (target == null || target.OwnerId == ownerId || !target.IsMine) continue;

				target.TakeDamage(damageType, ref damage, armorPierce, out bool damageExhausted);

				if (damageExhausted) break;
			}
		}
	}
}
}