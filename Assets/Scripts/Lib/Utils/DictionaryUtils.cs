using System;
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
			AddEntry(pair.Key, pair.Value, dst);
		}
	}

	public static void AddScaledDictionary<T>(IReadOnlyDictionary<T, float> src, float scale, IDictionary<T, float> dst)
	{
		foreach (KeyValuePair<T, float> pair in src)
		{
			AddEntry(pair.Key, pair.Value * scale, dst);
		}
	}

	public static void AddEntry<T>(T key, float value, IDictionary<T, float> dict)
	{
		dict[key] = dict.TryGetValue(key, out float current) ? current + value : value;
	}

	public static void MergeDictionary<TKey, TValue>(
		IReadOnlyDictionary<TKey, TValue> src, IDictionary<TKey, TValue> dst,
		Func<TValue, TValue, TValue> merge
	)
	{
		foreach (KeyValuePair<TKey, TValue> pair in src)
		{
			dst[pair.Key] = dst.TryGetValue(pair.Key, out TValue current) ? merge(current, pair.Value) : pair.Value;
		}
	}
}
}