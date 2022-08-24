using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Photon.Pun;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Foundation.Physics;
using Syy1125.OberthEffect.Lib.Utils;
using UnityEngine;

namespace Syy1125.OberthEffect.CombatSystem
{
[RequireComponent(typeof(IProjectileController))]
public class DamagingProjectile : MonoBehaviour, IProjectileLifecycleListener
{
	private IProjectileController _controller;

	private float _damage;
	private DamagePattern _damagePattern;
	private string _damageTypeId;
	private float _armorPierce;
	private float _explosionRadius;
	private Func<float> _getDamageModifier;
	private float _deathTime;
	private bool _expectExploded;

	private Coroutine _lateFixedUpdate;

	private List<ReferenceFrameProvider.RayStep[]> _allSteps = new();
	private List<RaycastHit2D> _hitResults = new();

#if UNITY_EDITOR
	private List<Tuple<Vector2, Vector2>> _tracePoints = new();
#endif

	private void Awake()
	{
		_controller = GetComponent<IProjectileController>();
	}

	public void Init(
		float maxDamage, DamagePattern damagePattern, string damageType, float armorPierce, float explosionRadius,
		Func<float> getDamageModifier, float lifetime
	)
	{
		_damage = maxDamage;
		_damagePattern = damagePattern;
		_damageTypeId = damageType;
		_armorPierce = Mathf.Clamp(armorPierce, 1f, 10f);
		_explosionRadius = explosionRadius;
		_getDamageModifier = getDamageModifier;
		_deathTime = Time.time + lifetime;

		_expectExploded = false;
	}

	public void AfterSpawn()
	{
		_lateFixedUpdate = StartCoroutine(LateFixedUpdate());
	}

	private IEnumerator LateFixedUpdate()
	{
		Vector2 prevPosition = transform.position;
		float prevTime = Time.time;
		yield return new WaitForFixedUpdate();

		while (isActiveAndEnabled)
		{
			Vector2 currentPosition = transform.position;

			var steps = GetRaySteps(prevPosition, currentPosition);

			if (Time.time >= _deathTime)
			{
				float timeFraction = Mathf.InverseLerp(prevTime, Time.time, _deathTime);
				if (steps != null)
				{
					TraceRaySteps(steps, timeFraction);
				}

				// If we have exhausted our damage, gameObject would've been deactivated.
				if (gameObject.activeSelf)
				{
					transform.position = Vector2.Lerp(prevPosition, currentPosition, timeFraction);
					LifetimeDespawn();
				}
			}
			else
			{
				if (steps != null)
				{
					TraceRaySteps(steps, 1f);
				}
			}

			prevPosition = currentPosition;
			prevTime = Time.time;
			yield return new WaitForFixedUpdate();
		}
	}

	public void BeforeDespawn()
	{
		StopCoroutine(_lateFixedUpdate);
	}

	#region Raytracing

	private IEnumerable<ReferenceFrameProvider.RayStep> GetRaySteps(
		Vector2 prevPosition, Vector2 currentPosition
	)
	{
		_allSteps.Clear();

		foreach (ReferenceFrameProvider referenceFrame in ReferenceFrameProvider.ReferenceFrames)
		{
			if (!referenceFrame.IsMine) continue;
			if (referenceFrame.gameObject == gameObject) continue;

			var radiusProvider = referenceFrame.GetComponent<ICollisionRadiusProvider>();
			if (radiusProvider == null) continue;

			float approachSqrDistance = referenceFrame.GetMinApproachSqrDistance(prevPosition, currentPosition);
			if (approachSqrDistance <= radiusProvider.GetCollisionSqrRadius())
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

	private void TraceRaySteps(IEnumerable<ReferenceFrameProvider.RayStep> steps, float timeLimit)
	{
		var warningBuilder = new StringBuilder();

		bool damageChanged = false;

		foreach (var step in steps)
		{
#if UNITY_EDITOR
			_tracePoints.Add(Tuple.Create(step.WorldStart, step.WorldEnd));
#endif

			if (step.T > timeLimit) return;

			Vector2 direction = step.WorldEnd - step.WorldStart;
			int hits = Physics2D.Raycast(
				step.WorldStart, direction, LayerConstants.WeaponHitFilter, _hitResults, direction.magnitude
			);

			if (hits >= 5)
			{
				warningBuilder.AppendLine($"Raycast from {step.WorldStart} to {step.WorldEnd} resulted in {hits} hits");
			}

			for (int i = 0; i < hits; i++)
			{
				var hitResult = ResolveHit(step, _hitResults[i]);

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
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		if (damageChanged)
		{
			_controller.InvokeProjectileRpc(
				typeof(DamagingProjectile), nameof(SetRemainingDamage), RpcTarget.Others, _damage
			);
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
		var hitTransform = hit.collider.transform;
		// hit.transform is actually the rigidbody's transform, but we want collider's transform.

		if (!hitTransform.IsChildOf(step.Parent)) return HitResult.Ignored;

		var target = hitTransform.GetComponentInParent<IDamageable>();
		if (target == null) return HitResult.Ignored;
		if (target.OwnerId == _controller.OwnerId) return HitResult.Ignored;

		if (!target.IsMine)
		{
			// Not mine to handle
			return HitResult.Stop;
		}

		// At this point, we've established that damage should be applied and that this client should be responsible for calculating damage.

		float damageModifier = _getDamageModifier?.Invoke() ?? 1f;
		if (_damagePattern == DamagePattern.Explosive)
		{
			// Explosive damage special case
			Vector3 explosionCenter = hit.point;
			ReferenceFrameProvider referenceFrame = hitTransform.GetComponentInParent<ReferenceFrameProvider>();
			int? referenceFrameId = null;
			if (referenceFrame != null)
			{
				referenceFrameId = referenceFrame.photonView.ViewID;
				explosionCenter = referenceFrame.transform.InverseTransformPoint(explosionCenter);
			}

			ExplosionManager.Instance.CreateExplosionAt(
				referenceFrameId, explosionCenter, _explosionRadius, _damage * damageModifier, _damageTypeId,
				_controller.OwnerId
			);
			DamageExhaustedDespawn(hit.point);

			return HitResult.Stop;
		}
		else
		{
			// Normal projectile damage behaviour
			float effectiveDamage = _damage * damageModifier;
			target.TakeDamage(_damageTypeId, ref effectiveDamage, _armorPierce, out bool damageExhausted);
			_damage = effectiveDamage / damageModifier;

			if (damageExhausted)
			{
				DamageExhaustedDespawn(hit.point);
				return HitResult.DamageExhausted;
			}
			else
			{
				return HitResult.DamageApplied;
			}
		}
	}

	#endregion

	[PunRPC]
	private void SetRemainingDamage(float damage)
	{
		_damage = damage;
	}

	private void DamageExhaustedDespawn(Vector3 endPoint)
	{
		transform.position = endPoint;
		_controller.RequestDestroyProjectile();
	}

	public void LifetimeDespawn()
	{
		if (_controller.IsMine)
		{
			if (_damagePattern == DamagePattern.Explosive && !_expectExploded)
			{
				float damageModifier = _getDamageModifier?.Invoke() ?? 1f;
				ExplosionManager.Instance.CreateExplosionAt(
					null, transform.position,
					_explosionRadius, _damage * damageModifier, _damageTypeId, _controller.OwnerId
				);
			}

			_controller.RequestDestroyProjectile();
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