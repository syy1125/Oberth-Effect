using System;
using System.Collections.Generic;
using System.Reflection;

namespace Syy1125.OberthEffect.Utils
{
public static class ConfigUtils
{
	private static readonly Dictionary<Type, string> KeyCache = new Dictionary<Type, string>();

	public static string GetConfigKey(Type type)
	{
		if (!KeyCache.TryGetValue(type, out string value))
		{
			var field = type.GetField("CONFIG_KEY", BindingFlags.Public | BindingFlags.Static);

			if (field == null || field.FieldType != typeof(string))
			{
				throw new ArgumentException($"Type {type} does not have `CONFIG_KEY` as a `public const string` field");
			}

			value = (string) field.GetValue(null);

			if (KeyCache.ContainsValue(value))
			{
				throw new ArgumentException($"Config key \"{value}\" for {type} already exists on a different type");
			}

			KeyCache.Add(type, value);
		}

		return value;
	}
}
}