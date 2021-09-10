using System;
using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation
{
public class CelestialBody : MonoBehaviourPun, IDamageable
{
	public float Mass;

	private void Start()
	{
		var circleCollider = gameObject.AddComponent<CircleCollider2D>();
		var pointEffector = gameObject.AddComponent<PointEffector2D>();
		
		circleCollider.radius = Mathf.Sqrt(Mass) / 0.001f;
		circleCollider.isTrigger = true;
		circleCollider.usedByEffector = true;

		pointEffector.forceMagnitude = -Mass;
		pointEffector.forceMode = EffectorForceMode2D.InverseSquared;
	}

	public bool IsMine => true;
	public int OwnerId => photonView.OwnerActorNr;

	public Tuple<Vector2, Vector2> GetExplosionDamageBounds()
	{
		return new Tuple<Vector2, Vector2>(Vector2.zero, Vector2.zero);
	}

	public void TakeDamage(
		DamageType damageType, ref float damage, float armorPierce, out bool damageExhausted
	)
	{
		damageExhausted = true;
	}

	public void RequestBeamDamage(
		DamageType damageType, float damage, float armorPierce, int ownerId, Vector2 beamStart, Vector2 beamEnd
	)
	{}
}
}