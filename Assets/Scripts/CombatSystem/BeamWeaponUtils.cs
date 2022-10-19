using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Physics;
using UnityEngine;

namespace Syy1125.OberthEffect.CombatSystem
{
public static class BeamWeaponUtils
{
	private static readonly List<RaycastHit2D> hits = new();

	public static void HandleBeamDamage(
		string damageType, float damage, float armorPierce, int ownerId,
		int? referenceFrameId, Vector2 beamStart, Vector2 beamEnd
	)
	{
		if (referenceFrameId != null)
		{
			var referenceFrame = PhotonView.Find(referenceFrameId.Value)?.GetComponent<ReferenceFrameProvider>();

			if (referenceFrame != null)
			{
				beamStart = referenceFrame.transform.TransformPoint(beamStart);
				beamEnd = referenceFrame.transform.TransformPoint(beamEnd);
			}
			else
			{
				Debug.LogError($"Failed to find reference frame with id {referenceFrameId}");
				// Avoid having a random beam shoot out around the origin. Don't do anything.
				return;
			}
		}

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