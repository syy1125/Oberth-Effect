using System;
using System.Collections;
using System.Collections.Generic;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Utils;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
public class ExplosionManager : MonoBehaviour
{
	public static ExplosionManager Instance { get; private set; }

	[Header("Damage Calculation")]
	public LayerMask AffectedLayers;

	[Header("Visual Effect")]
	public GameObject ExplosionEffectPrefab;
	public AnimationCurve AlphaCurve;
	public float EffectDuration;

	private Stack<GameObject> _visualEffectPool;

	private ContactFilter2D _contactFilter;
	private List<Collider2D> _colliders;
	private IDamageable[] _targets;

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

		_visualEffectPool = new Stack<GameObject>();

		_contactFilter = new ContactFilter2D
		{
			layerMask = AffectedLayers,
			useLayerMask = true
		};
		_colliders = new List<Collider2D>();
		_targets = new IDamageable[0];
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}


	public void CreateExplosionAt(Vector3 center, float radius, float damage)
	{
		DealExplosionDamageAt(center, radius, damage);
		PlayEffectAt(center, radius);
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
	/// <returns></returns>
	public static float CalculateDamageFactor(
		Vector2 minPos, Vector2 maxPos, Vector2 explosionCenter, float maxRadius, int gridResolution = 100
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
				float distance = (position - explosionCenter).magnitude;

				if (distance < maxRadius)
				{
					baseFactor += 1f / Mathf.Max(10 * distance / maxRadius, 1f);
				}
			}
		}

		return baseFactor * unitArea;
	}

	private struct CalculateDamageFactorJob : IJobParallelFor
	{
		// Inputs
		[ReadOnly]
		public NativeArray<float> MinX;
		[ReadOnly]
		public NativeArray<float> MinY;
		[ReadOnly]
		public NativeArray<float> MaxX;
		[ReadOnly]
		public NativeArray<float> MaxY;
		[ReadOnly]
		public NativeArray<float> CenterX;
		[ReadOnly]
		public NativeArray<float> CenterY;
		[ReadOnly]
		public float Radius;

		// Output
		public NativeArray<float> Results;

		public void Execute(int index)
		{
			Vector2 min = new Vector2(MinX[index], MinY[index]);
			Vector2 max = new Vector2(MaxX[index], MaxY[index]);
			Vector2 center = new Vector2(CenterX[index], CenterY[index]);

			Results[index] = CalculateDamageFactor(min, max, center, Radius);
		}
	}

	private void DealExplosionDamageAt(Vector3 center, float radius, float damage)
	{
		int colliderCount = Physics2D.OverlapCircle(center, radius, _contactFilter, _colliders);

		// We're allocating enough space for `colliderCount`, but note that only the first `count` items are actually valid
		if (_targets.Length < colliderCount) _targets = new IDamageable[_targets.Length + colliderCount];
		NativeArray<float> minX = new NativeArray<float>(colliderCount, Allocator.TempJob);
		NativeArray<float> minY = new NativeArray<float>(colliderCount, Allocator.TempJob);
		NativeArray<float> maxX = new NativeArray<float>(colliderCount, Allocator.TempJob);
		NativeArray<float> maxY = new NativeArray<float>(colliderCount, Allocator.TempJob);
		NativeArray<float> centerX = new NativeArray<float>(colliderCount, Allocator.TempJob);
		NativeArray<float> centerY = new NativeArray<float>(colliderCount, Allocator.TempJob);
		NativeArray<float> results = new NativeArray<float>(colliderCount, Allocator.TempJob);

		try
		{
			int count = 0;
			for (int i = 0; i < colliderCount; i++)
			{
				IDamageable target = ComponentUtils.GetBehaviourInParent<IDamageable>(_colliders[i].transform);

				if (target == null || !target.IsMine) continue;

				_targets[count] = target;

				(Vector2 boundsMin, Vector2 boundsMax) = target.GetExplosionDamageBounds();
				minX[count] = boundsMin.x;
				minY[count] = boundsMin.y;
				maxX[count] = boundsMax.x;
				maxY[count] = boundsMax.y;
				Vector3 localExplosionCenter = target.transform.InverseTransformPoint(center);
				centerX[count] = localExplosionCenter.x;
				centerY[count] = localExplosionCenter.y;

				count++;
			}

			var job = new CalculateDamageFactorJob
			{
				MinX = minX,
				MinY = minY,
				MaxX = maxX,
				MaxY = maxY,
				CenterX = centerX,
				CenterY = centerY,
				Radius = radius,
				Results = results
			};

			JobHandle handle = job.Schedule(count, 2);
			handle.Complete();

			float d = damage * 100 / (19 * Mathf.PI * radius * radius);

			for (int i = 0; i < count; i++)
			{
				float effectiveDamage = d * results[i];
				_targets[i].TakeDamage(DamageType.Explosive, ref effectiveDamage, 1f, out bool _);
			}
		}
		finally
		{
			minX.Dispose();
			minY.Dispose();
			maxX.Dispose();
			maxY.Dispose();
			centerX.Dispose();
			centerY.Dispose();
			results.Dispose();
		}
	}

	#endregion

	#region Visual Effects

	public void PlayEffectAt(Vector3 position, float size)
	{
		StartCoroutine(DoPlayEffect(position, size));
	}

	private IEnumerator DoPlayEffect(Vector2 position, float size)
	{
		GameObject effect = _visualEffectPool.Count > 0
			? _visualEffectPool.Pop()
			: Instantiate(ExplosionEffectPrefab);

		effect.transform.position = position;
		effect.transform.localScale = new Vector3(size, size, 1f);
		effect.SetActive(true);

		SpriteRenderer sprite = effect.GetComponent<SpriteRenderer>();
		Color color = sprite.color;
		float startTime = Time.time;
		float endTime = startTime + EffectDuration;

		while (Time.time < endTime)
		{
			if (sprite == null) yield break;

			float progress = Mathf.InverseLerp(startTime, endTime, Time.time);
			color.a = AlphaCurve.Evaluate(progress);
			sprite.color = color;

			yield return null;
		}

		effect.SetActive(false);
		_visualEffectPool.Push(effect);
	}

	#endregion
}
}