using System;
using System.Collections;
using UnityEngine;

namespace Syy1125.OberthEffect.Components
{
public static class CoroutineUtils
{
	private static IEnumerator LerpOverTime<T>(
		T startValue, T endValue, float duration, Func<float> getTime, Func<T, T, float, T> lerp, Action<T> action
	)
	{
		float startTime = getTime();
		float endTime = startTime + duration;

		action(startValue);

		while (getTime() < endTime)
		{
			action(lerp(startValue, endValue, Mathf.InverseLerp(startTime, endTime, getTime())));
			yield return null;
		}

		action(endValue);
	}

	public static IEnumerator LerpOverTime(float startValue, float endValue, float duration, Action<float> action)
		=> LerpOverTime(startValue, endValue, duration, () => Time.time, Mathf.Lerp, action);

	public static IEnumerator LerpOverTime(Vector3 startValue, Vector3 endValue, float duration, Action<Vector3> action)
		=> LerpOverTime(startValue, endValue, duration, () => Time.time, Vector3.Lerp, action);

	public static IEnumerator LerpOverUnscaledTime(
		float startValue, float endValue, float duration, Action<float> action
	)
		=> LerpOverTime(startValue, endValue, duration, () => Time.unscaledTime, Mathf.Lerp, action);
}
}