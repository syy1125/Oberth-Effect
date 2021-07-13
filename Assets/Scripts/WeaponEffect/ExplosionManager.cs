using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
public class ExplosionManager : MonoBehaviour
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

		_visualEffectPool = new Stack<GameObject>();
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

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

	public void CreateExplosionAt(Vector3 center, float damage, float radius)
	{
		int resultCount = Physics2D.OverlapCircle(center, radius, new ContactFilter2D().NoFilter(), _colliders);
	}

	public void PlayEffectAt(Vector3 position, float size)
	{
		StartCoroutine(DoPlayEffect(position, size));
	}

	private IEnumerator DoPlayEffect(Vector3 position, float size)
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
}
}