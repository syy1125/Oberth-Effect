using System;
using System.Collections.Generic;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Lib.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Syy1125.OberthEffect.Simulation
{
public class CameraScreenShake : MonoBehaviour
{
	public enum DecayCurve
	{
		Linear,
		Quadratic,
		Cubic
	}

	private struct ScreenShakeInstance
	{
		public float StartTime;
		public float EndTime;
		public float Strength;
		public DecayCurve Decay;
	}

	public float Frequency = 5f;

	private LinkedList<ScreenShakeInstance> _instances;
	private float _perlinOffset;
	private float _screenShakeMultiplier;

	private void Awake()
	{
		_instances = new LinkedList<ScreenShakeInstance>();
	}

	private void Start()
	{
		_perlinOffset = -Random.value;
		_screenShakeMultiplier = PlayerPrefs.GetFloat(PropertyKeys.SCREEN_SHAKE_MULTIPLIER, 1f);
	}

	public void AddInstance(float strength, float duration, DecayCurve decay)
	{
		float startTime = Time.time;
		float endTime = startTime + duration;

		_instances.AddLast(
			new ScreenShakeInstance { StartTime = startTime, EndTime = endTime, Strength = strength, Decay = decay }
		);
	}

	private void Update()
	{
		if (_instances.Count > 0)
		{
			float time = Time.time;
			float totalStrength = 0f;

			for (var node = _instances.First; node != null;)
			{
				var next = node.Next;

				if (time < node.Value.EndTime)
				{
					float decay = MathUtils.Remap(time, node.Value.StartTime, node.Value.EndTime, 1f, 0f);
					totalStrength += node.Value.Strength
					                 * node.Value.Decay switch
					                 {
						                 DecayCurve.Linear => decay,
						                 DecayCurve.Quadratic => decay * decay,
						                 DecayCurve.Cubic => decay * decay * decay,
						                 _ => throw new ArgumentOutOfRangeException()
					                 };
				}
				else
				{
					_instances.Remove(node);
				}

				node = next;
			}

			Vector2 displacement = new Vector2(
				Mathf.PerlinNoise(time * Frequency, _perlinOffset) - 0.5f,
				Mathf.PerlinNoise(_perlinOffset, time * Frequency) - 0.5f
			);
			displacement *= 2 * totalStrength * _screenShakeMultiplier;

			transform.localPosition = displacement;
		}
		else
		{
			transform.localPosition = Vector3.zero;
		}
	}
}
}