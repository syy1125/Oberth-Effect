using System;
using System.Collections.Generic;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Lib.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Syy1125.OberthEffect.Common
{
public class CameraScreenShake : MonoBehaviour
{
	private struct ScreenShakeInstance
	{
		public float StartTime;
		public float EndTime;
		public float Strength;
		public ScreenShakeDecayCurve Decay;
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

	public void AddInstance(float strength, float duration, ScreenShakeDecayCurve decay)
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
			// Strength is added in quadrature to reduce extreme cases of screen shake
			float totalStrength = 0f;

			for (var node = _instances.First; node != null;)
			{
				var next = node.Next;

				if (time < node.Value.EndTime)
				{
					float decay = MathUtils.Remap(time, node.Value.StartTime, node.Value.EndTime, 1f, 0f);
					float instanceStrength = node.Value.Strength
					                         * node.Value.Decay switch
					                         {
						                         ScreenShakeDecayCurve.Linear => decay,
						                         ScreenShakeDecayCurve.Quadratic => decay * decay,
						                         ScreenShakeDecayCurve.Cubic => decay * decay * decay,
						                         _ => throw new ArgumentOutOfRangeException()
					                         };

					totalStrength += instanceStrength * instanceStrength;
				}
				else
				{
					_instances.Remove(node);
				}

				node = next;
			}

			totalStrength = Mathf.Sqrt(totalStrength);

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