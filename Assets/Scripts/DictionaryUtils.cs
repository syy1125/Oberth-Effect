using System.Collections.Generic;

namespace Syy1125.OberthEffect
{
public static class DictionaryUtils
{
	public static void AddDictionaries<T>(IEnumerable<IDictionary<T, float>> src, IDictionary<T, float> dst)
	{
		foreach (IDictionary<T, float> d in src)
		{
			foreach (KeyValuePair<T, float> pair in d)
			{
				dst[pair.Key] = dst.TryGetValue(pair.Key, out float value) ? value + pair.Value : pair.Value;
			}
		}
	}
}
}