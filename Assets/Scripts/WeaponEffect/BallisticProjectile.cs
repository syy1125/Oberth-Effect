using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Photon.Pun;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Common.Physics;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
[Serializable]
public struct BallisticProjectileConfig
{
	public Vector2 ColliderSize;
	public float Damage;
	public DamageType DamageType;
	public float ArmorPierce; // Note that explosive damage will always have damage output value of 1
	public float ExplosionRadius; // Only relevant for explosive damage
	public float Lifetime;

	public RendererSpec[] Renderers;
}

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BallisticProjectile : MonoBehaviourPun, IPunInstantiateMagicCallback
{
	public BoxCollider2D ProjectileCollider;

	private BallisticProjectileConfig _config;
	private List<ReferenceFrameProvider.RayStep[]> _allSteps = new List<ReferenceFrameProvider.RayStep[]>();
	private bool _expectExploded;

#if UNITY_EDITOR
	private List<Tuple<Vector2, Vector2>> _tracePoints = new List<Tuple<Vector2, Vector2>>();
#endif

	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		object[] instantiationData = info.photonView.InstantiationData;
		_config = JsonUtility.FromJson<BallisticProjectileConfig>((string) instantiationData[0]);
		_config.ArmorPierce = Mathf.Clamp(_config.ArmorPierce, 1, 10);

		ProjectileCollider.size = _config.ColliderSize;

		RendererHelper.AttachRenderers(transform, _config.Renderers);
	}

	private void Start()
	{
		StartCoroutine(LateFixedUpdate());
		Invoke(nameof(LifetimeDespawn), _config.Lifetime);
	}

	private IEnumerator LateFixedUpdate()
	{
		Vector2 prevPosition = transform.position;
		yield return new WaitForFixedUpdate();

		while (enabled)
		{
			Vector2 currentPosition = transform.position;

			var steps = GetRaySteps(prevPosition, currentPosition);

			if (steps != null)
			{
				TraceRaySteps(steps);
			}

			prevPosition = currentPosition;
			yield return new WaitForFixedUpdate();
		}
	}

	private IEnumerable<ReferenceFrameProvider.RayStep> GetRaySteps(
		Vector2 prevPosition, Vector2 currentPosition
	)
	{
		_allSteps.Clear();

		foreach (ReferenceFrameProvider referenceFrame in ReferenceFrameProvider.ReferenceFrames)
		{
			var radiusProvider = referenceFrame.GetComponent<ICollisionRadiusProvider>();
			if (radiusProvider == null) continue;

			float approachDistance = referenceFrame.EstimateMinApproachDistance(prevPosition, currentPosition);
			if (approachDistance <= radiusProvider.GetCollisionRadius())
			{
				_allSteps.Add(referenceFrame.GetRaySteps(prevPosition, currentPosition));
			}
		}

		return _allSteps.Count switch
		{
			0 => null,
			1 => _allSteps[0],
			_ => _allSteps.MergeSorted(step => step.T)
		};
	}

	private void TraceRaySteps(IEnumerable<ReferenceFrameProvider.RayStep> steps)
	{
		StringBuilder warningBuilder = new StringBuilder();

		RaycastHit2D[] results = new RaycastHit2D[5];
		bool damageChanged = false;

		foreach (var step in steps)
		{
#if UNITY_EDITOR
			_tracePoints.Add(Tuple.Create(step.WorldStart, step.WorldEnd));
#endif

			Vector2 direction = step.WorldEnd - step.WorldStart;
			int hits = Physics2D.Raycast(
				step.WorldStart, direction, LayerConstants.WeaponHitFilter, results, direction.magnitude
			);

			if (hits >= 5)
			{
				warningBuilder.AppendLine($"Raycast from {step.WorldStart} to {step.WorldEnd} resulted in {hits} hits");
			}

			for (int i = 0; i < hits; i++)
			{
				var hitResult = ResolveHit(step, results[i]);

				switch (hitResult)
				{
					case HitResult.Ignored:
						break;
					case HitResult.DamageApplied:
						damageChanged = true;
						break;
					case HitResult.DamageExhausted:
					case HitResult.Stop:
						// ResolveHit has already taken appropriate action.
						return;
				}
			}
		}

		if (damageChanged)
		{
			photonView.RPC(nameof(SetRemainingDamage), RpcTarget.Others, _config.Damage);
		}
	}

	private enum HitResult
	{
		Ignored,
		DamageApplied,
		DamageExhausted,
		Stop
	}

	private HitResult ResolveHit(ReferenceFrameProvider.RayStep step, RaycastHit2D hit)
	{
		if (hit.collider.isTrigger) return HitResult.Ignored;
		var hitTransform =
			hit.collider
				.transform; // hit.transform is actually the rigidbody's transform, but we want collider's transform.

		if (!hitTransform.IsChildOf(step.Parent)) return HitResult.Ignored;

		IDamageable target = hitTransform.GetComponentInParent<IDamageable>();
		if (target == null) return HitResult.Ignored;
		if (target.OwnerId == photonView.OwnerActorNr) return HitResult.Ignored;

		if (!target.IsMine)
		{
			if (_config.DamageType == DamageType.Explosive)
			{
				gameObject.SetActive(false);
				_expectExploded = true;
			}

			return HitResult.Stop;
		}

		// At this point, we've established that damage should be applied and that this client should be responsible for calculating damage.

		if (_config.DamageType == DamageType.Explosive)
		{
			// Explosive damage special case
			ExplosionManager.Instance.CreateExplosionAt(
				hit.point, _config.ExplosionRadius, _config.Damage, photonView.OwnerActorNr
			);
			photonView.RPC(nameof(DestroyProjectile), photonView.Owner);
			return HitResult.Stop;
		}
		else
		{
			// Normal projectile damage behaviour
			target.TakeDamage(_config.DamageType, ref _config.Damage, _config.ArmorPierce, out bool damageExhausted);

			if (damageExhausted)
			{
				gameObject.SetActive(false);
				photonView.RPC(nameof(DestroyProjectile), photonView.Owner);
				return HitResult.DamageExhausted;
			}
			else
			{
				return HitResult.DamageApplied;
			}
		}
	}

	private void LifetimeDespawn()
	{
		gameObject.SetActive(false);

		if (photonView.IsMine)
		{
			if (_config.DamageType == DamageType.Explosive && !_expectExploded)
			{
				ExplosionManager.Instance.CreateExplosionAt(
					transform.position, _config.ExplosionRadius, _config.Damage, photonView.OwnerActorNr
				);
			}

			PhotonNetwork.Destroy(gameObject);
		}
	}

	[PunRPC]
	private void SetRemainingDamage(float damage)
	{
		_config.Damage = damage;
	}

	[PunRPC]
	private void DestroyProjectile()
	{
		if (photonView.IsMine)
		{
			PhotonNetwork.Destroy(gameObject);
		}
	}

#if UNITY_EDITOR
	private void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Gizmos.matrix = Matrix4x4.identity;

		foreach (Tuple<Vector2, Vector2> tuple in _tracePoints)
		{
			Gizmos.DrawLine(tuple.Item1, tuple.Item2);
		}
	}
#endif
}
}