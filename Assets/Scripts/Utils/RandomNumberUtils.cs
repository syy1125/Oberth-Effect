using UnityEngine;

namespace Syy1125.OberthEffect.Utils
{
public static class RandomNumberUtils
{
	// Reference: https://stackoverflow.com/questions/5817490/implementing-box-mueller-random-number-generator-in-c-sharp
	public static float NextGaussian()
	{
		float u, v, s;

		do
		{
			u = Random.Range(-1f, 1f);
			v = Random.Range(-1f, 1f);
			s = u * u + v * v;
		}
		while (s >= 1);

		float fac = Mathf.Sqrt(-2f * Mathf.Log(s) / s);
		return u * fac;
	}
}
}