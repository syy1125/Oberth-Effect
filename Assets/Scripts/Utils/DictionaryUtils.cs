using System.Collections.Generic;

namespace Syy1125.OberthEffect.Utils
{
public static class DictionaryUtils
{
	public static void SumDictionaries<T>(IEnumerable<IDictionary<T, float>> src, IDictionary<T, float> dst)
	{
		foreach (IDictionary<T, float> d in src)
		{
			AddDictionary(d, dst);
		}
	}

	public static void AddDictionary<T>(IDictionary<T, float> src, IDictionary<T, float> dst)
	{
		foreach (KeyValuePair<T, float> pair in src)
		{
			dst[pair.Key] = dst.TryGetValue(pair.Key, out float value) ? value + pair.Value : pair.Value;
		}
	}
}
}