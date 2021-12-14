using System.Collections.Generic;

namespace Syy1125.OberthEffect.Lib.Utils
{
public static class DictionaryUtils
{
	public static void SumDictionaries<T>(IEnumerable<IReadOnlyDictionary<T, float>> src, IDictionary<T, float> dst)
	{
		foreach (IReadOnlyDictionary<T, float> d in src)
		{
			AddDictionary(d, dst);
		}
	}

	public static void AddDictionary<T>(IReadOnlyDictionary<T, float> src, IDictionary<T, float> dst)
	{
		foreach (KeyValuePair<T, float> pair in src)
		{
			dst[pair.Key] = dst.TryGetValue(pair.Key, out float value) ? value + pair.Value : pair.Value;
		}
	}
}
}