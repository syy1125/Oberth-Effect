using System;
using System.Collections.Generic;
using UnityEngine;

namespace Syy1125.OberthEffect.Lib.Utils
{
public static class UnitUtils
{
	private static List<string> _prefixes = new List<string> { "µ", "m", "", "k", "M", "G", };
	private const int INITIAL_PREFIX_INDEX = 2;

	public static Tuple<float, string> GetMetricPrefix(float value)
	{
		if (Mathf.Approximately(value, 0f))
		{
			return new Tuple<float, string>(0, "");
		}

		float sign = Mathf.Sign(value);
		value = Mathf.Abs(value);
		int index = INITIAL_PREFIX_INDEX;

		while (value < 1 && index > 0)
		{
			value *= 1000;
			index--;
		}

		while (value > 1000 && index < _prefixes.Count - 1)
		{
			value /= 1000;
			index++;
		}

		return new Tuple<float, string>(sign * value, _prefixes[index]);
	}
}
}