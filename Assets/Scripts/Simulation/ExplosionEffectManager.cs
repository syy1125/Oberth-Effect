using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation
{
public class ExplosionEffectManager : MonoBehaviour
{
	public static ExplosionEffectManager Instance { get; private set; }

	public GameObject ExplosionEffectPrefab;
	public AnimationCurve AlphaCurve;
	public float EffectDuration;

	private Stack<GameObject> _pool;

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

		_pool = new Stack<GameObject>();
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void PlayEffectAt(Vector3 position, float size)
	{
		StartCoroutine(DoPlayEffect(position, size));
	}

	private IEnumerator DoPlayEffect(Vector3 position, float size)
	{
		GameObject effect = _pool.Count > 0
			? _pool.Pop()
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
		_pool.Push(effect);
	}
}
}