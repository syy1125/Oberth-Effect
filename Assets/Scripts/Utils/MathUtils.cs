using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Syy1125.OberthEffect.Utils
{
public static class MathUtils
{
	public static float Remap(float value, float oldMin, float oldMax, float newMin, float newMax)
	{
		return Mathf.Lerp(newMin, newMax, Mathf.InverseLerp(oldMin, oldMax, value));
	}

	// Reference: https://stackoverflow.com/questions/5817490/implementing-box-mueller-random-number-generator-in-c-sharp
	public static float RandomGaussian()
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

	#region Inverse Normal Function Support

	private static readonly float[] InverseP0 =
	{
		-59.963350101410789f,
		98.001075418599967f,
		-56.676285746907027f,
		13.931260938727968f,
		-1.2391658386738125f
	};

	private static readonly float[] InverseQ0 =
	{
		1.9544885833814176f,
		4.6762791289888153f,
		86.360242139089053f,
		-225.46268785411937f,
		200.26021238006066f,
		-82.037225616833339f,
		15.90562251262117f,
		-1.1833162112133f
	};

	private static readonly float[] InverseP1 =
	{
		4.0554489230596245f,
		31.525109459989388f,
		57.162819224642128f,
		44.080507389320083f,
		14.684956192885803f,
		2.1866330685079025f,
		-0.14025607917135449f,
		-0.035042462682784818f,
		-0.00085745678515468545f
	};

	private static readonly float[] InverseQ1 =
	{
		15.779988325646675f,
		45.390763512887922f,
		41.317203825467203f,
		15.04253856929075f,
		2.5046494620830941f,
		-0.14218292285478779f,
		-0.038080640769157827f,
		-0.00093325948089545744f
	};

	private static readonly float[] InverseP2 =
	{
		3.2377489177694603f,
		6.9152288906898418f,
		3.9388102529247444f,
		1.3330346081580755f,
		0.20148538954917908f,
		0.012371663481782003f,
		0.00030158155350823543f,
		2.6580697468673755E-06f,
		6.2397453918498331E-09f
	};

	private static readonly float[] InverseQ2 =
	{
		6.02427039364742f,
		3.6798356385616087f,
		1.3770209948908132f,
		0.21623699359449663f,
		0.013420400608854318f,
		0.00032801446468212774f,
		2.8924786474538068E-06f,
		6.7901940800998127E-09f
	};

	// Following two functions taken from https://github.com/accord-net/framework/blob/master/Sources/Accord.Math/Special.cs#L234
	// Support functions needed for inverse normal cdf
	private static float PolyEval(float x, IReadOnlyList<float> coef, int n)
	{
		float ans = coef[0];

		for (int i = 1; i < n; i++)
		{
			ans = ans * x + coef[i];
		}

		return ans;
	}

	private static float Poly1Eval(float x, IReadOnlyList<float> coef, int n)
	{
		float ans = x + coef[0];

		for (int i = 1; i < n; i++)
		{
			ans = ans * x + coef[i];
		}

		return ans;
	}

	#endregion

	// Taken from https://github.com/accord-net/framework/blob/master/Sources/Accord.Math/Functions/Normal.cs#L223
	// Accord.Net is a massive library and I only really needed this piece.
	public static float InverseNormal(float y)
	{
		if (y <= Mathf.Epsilon || y >= 1f - Mathf.Epsilon)
		{
			throw new ArgumentOutOfRangeException(nameof(y));
		}

		float s2pi = Mathf.Sqrt(2f * Mathf.PI);
		int code = 1;
		float x;

		if (y > 0.8646647167633873f)
		{
			y = 1f - y;
			code = 0;
		}

		if (y > 0.1353352832366127f)
		{
			y -= 0.5f;
			float y2 = y * y;
			x = y + y * ((y2 * PolyEval(y2, InverseP0, 4)) / Poly1Eval(y2, InverseQ0, 8));
			x *= s2pi;
			return x;
		}

		x = Mathf.Sqrt(-2f * Mathf.Log(y));
		float x0 = x - Mathf.Log(x) / x;
		float z = 1f / x;
		float x1;

		if (x < 8.0)
		{
			x1 = z * PolyEval(z, InverseP1, 8) / Poly1Eval(z, InverseQ1, 8);
		}
		else
		{
			x1 = z * PolyEval(z, InverseP2, 8) / Poly1Eval(z, InverseQ2, 8);
		}

		x = x0 - x1;

		if (code != 0)
		{
			x = -x;
		}

		return x;
	}
}
}