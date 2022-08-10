using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Foundation.Physics;
using Syy1125.OberthEffect.Spec;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;

namespace Syy1125.OberthEffect.CombatSystem
{
[RequireComponent(typeof(PhotonView))]
public class ExplosionManager : MonoBehaviourPun
{
	public static ExplosionManager Instance { get; private set; }

	[Header("Visual Effect")]
	public GameObject ExplosionEffectPrefab;
	public AnimationCurve AlphaCurve;
	public float EffectDuration;

	private Stack<GameObject> _visualEffectPool;

	private List<Collider2D> _colliders;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else if (Instance != this)
		{
			Debug.LogError($"Multiple ExplosionEffectManager were instantiated");
			Destroy(gameObject);
			return;
		}

		_visualEffectPool = new();
		_colliders = new();
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	/// <summary>
	/// Function to call when something explodes.
	/// </summary>
	/// <param name="referenceFrameId">If applicable, the reference frame that the explosion attaches to.</param>
	/// <param name="center">The center of the explosion. If `referenceFrameId` is specified, this is the local position relative to the reference frame's transform.</param>
	/// <param name="radius">Radius of the explosion.</param>
	/// <param name="damage">Base damage value of the explosion.</param>
	/// <param name="damageType">Damage type id of the explosion.</param>
	/// <param name="ownerId">Owner of the explosion. Used in friendly fire calculations.</param>
	public void CreateExplosionAt(
		int? referenceFrameId, Vector3 center, float radius, float damage, string damageType, int ownerId
	)
	{
		DoExplosionAt(referenceFrameId, center, radius, damage, damageType, true, ownerId);
		photonView.RPC(
			nameof(DoExplosionAt), RpcTarget.Others,
			referenceFrameId, center, radius, damage, damageType, false, ownerId
		);
	}

	[PunRPC]
	private void DoExplosionAt(
		int? referenceFrameId, Vector3 center, float radius, float damage, string damageType, bool isMine, int ownerId
	)
	{
		Vector2? visualVelocity = null;

		if (referenceFrameId != null)
		{
			var referenceFrame = PhotonView.Find(referenceFrameId.Value)?.GetComponent<ReferenceFrameProvider>();

			if (referenceFrame != null)
			{
				center = referenceFrame.transform.TransformPoint(center);
				visualVelocity = referenceFrame.GetVelocity();
			}
		}

		if (visualVelocity == null && ReferenceFrameProvider.MainReferenceFrame != null)
		{
			visualVelocity = ReferenceFrameProvider.MainReferenceFrame.GetVelocity();
		}

		DealExplosionDamageAt(center, radius, damage, damageType, isMine, ownerId);
		PlayEffectAt(center, radius, damageType, visualVelocity ?? Vector2.zero);
	}

	#region Damage Calculation

	/// <summary>
	/// Calculate the damage factor for the damage a block should receive from an explosion
	/// </summary>
	/// <param name="minPos">The minimum corner of the rectangle</param>
	/// <param name="maxPos">The maximum corner of the rectangle</param>
	/// <param name="explosionCenter">The center of the circle</param>
	/// <param name="maxRadius">The radius of the circle</param>
	/// <param name="gridResolution">How many grid points along each axis to use</param>
	/// <param name="containsPoint">Predicate to determine whether the point is contained within the damageable target</param>
	/// <returns></returns>
	public static float CalculateDamageFactor(
		Vector2 minPos, Vector2 maxPos, Vector2 explosionCenter, float maxRadius,
		int gridResolution = 100, Predicate<Vector2> containsPoint = null
	)
	{
		float area = (maxPos.x - minPos.x) * (maxPos.y - minPos.y);
		float unitArea = area / (gridResolution * gridResolution);
		float baseFactor = 0f;

		for (float x = 0.5f; x < gridResolution; x++)
		{
			for (float y = 0.5f; y < gridResolution; y++)
			{
				Vector2 position = new Vector2(
					Mathf.Lerp(minPos.x, maxPos.x, x / gridResolution),
					Mathf.Lerp(minPos.y, maxPos.y, y / gridResolution)
				);

				if (containsPoint != null && !containsPoint(position)) continue;

				float distance = (position - explosionCenter).magnitude;

				if (distance < maxRadius)
				{
					baseFactor += 1f / Mathf.Max(10 * distance / maxRadius, 1f);
				}
			}
		}

		return baseFactor * unitArea;
	}

	private void DealExplosionDamageAt(
		Vector3 center, float radius, float damage, string damageType, bool isMine, int explosionOwnerId
	)
	{
		int colliderCount = Physics2D.OverlapCircle(center, radius, LayerConstants.WeaponHitFilter, _colliders);
		var targets = _colliders
			.Take(colliderCount)
			.Select(c => c.GetComponentInParent<IDamageable>())
			.Distinct()
			.Where(target => target != null && target.OwnerId != explosionOwnerId)
			.ToList();

#if UNITY_EDITOR
		Debug.Log($"Explosion query found {colliderCount} colliders, mapped to {targets.Count} targets");
#endif

		// Deal double damage as generally, only half of the explosion will cover the target.
		float d = damage * 200 / (19 * Mathf.PI * radius * radius);

		foreach (IDamageable target in targets)
		{
			if (target is IDirectDamageable directDamageable)
			{
				if (!isMine) continue;

				(Vector2 minPos, Vector2 maxPos) = target.GetExplosionDamageBounds();
				Vector2 localCenter = target.transform.InverseTransformPoint(center);

				float effectiveDamage =
					d
					* CalculateDamageFactor(
						minPos, maxPos, localCenter, radius,
						target.GetExplosionGridResolution(),
						target.GetPointInBoundPredicate()
					);
				directDamageable.RequestDirectDamage(damageType, effectiveDamage, 1f);
			}
			else
			{
				if (!target.IsMine) continue;

				(Vector2 minPos, Vector2 maxPos) = target.GetExplosionDamageBounds();
				Vector2 localCenter = target.transform.InverseTransformPoint(center);

				float effectiveDamage =
					d
					* CalculateDamageFactor(
						minPos, maxPos, localCenter, radius,
						target.GetExplosionGridResolution(),
						target.GetPointInBoundPredicate()
					);
				target.TakeDamage(damageType, ref effectiveDamage, 1f, out bool _);
			}
		}
	}

	#endregion

	#region Visual Effects

	public void PlayEffectAt(Vector3 position, float size, string damageType, Vector2 velocity)
	{
		DamageTypeSpec damageTypeSpec = DamageTypeDatabase.Instance.GetSpec(damageType);

		StartCoroutine(DoPlayEffect(position, size, damageTypeSpec.GetDisplayColor(), velocity));
	}

	private IEnumerator DoPlayEffect(Vector2 position, float size, Color color, Vector2 velocity)
	{
		GameObject effect = _visualEffectPool.Count > 0
			? _visualEffectPool.Pop()
			: Instantiate(ExplosionEffectPrefab);

		effect.transform.position = position;
		effect.transform.localScale = new Vector3(size * 2, size * 2, 1f);
		effect.SetActive(true);

		var sprite = effect.GetComponent<SpriteRenderer>();
		if (sprite != null)
		{
			float startTime = Time.time;
			float endTime = startTime + EffectDuration;

			while (Time.time < endTime)
			{
				effect.transform.position = position + velocity * (Time.time - startTime);

				float progress = Mathf.InverseLerp(startTime, endTime, Time.time);
				color.a = AlphaCurve.Evaluate(progress);
				sprite.color = color;

				yield return new WaitForFixedUpdate();
			}
		}

		effect.SetActive(false);
		_visualEffectPool.Push(effect);
	}

	#endregion
}
}