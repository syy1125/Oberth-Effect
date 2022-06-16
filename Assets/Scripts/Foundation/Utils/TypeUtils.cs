using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Syy1125.OberthEffect.Foundation.Utils
{
public static class TypeUtils
{
	private static readonly Dictionary<Type, string> KeyCache = new();

	public static string GetClassKey(Type type)
	{
		if (!KeyCache.TryGetValue(type, out string value))
		{
			var field = type.GetField("CLASS_KEY", BindingFlags.Public | BindingFlags.Static);

			if (field == null || field.FieldType != typeof(string))
			{
				throw new ArgumentException($"Type {type} does not have `CLASS_KEY` as a `public const string` field");
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

	public static JObject ParseJson(string json)
	{
		JObject config = new JObject();

		try
		{
			config = JObject.Parse(json);
		}
		catch (JsonReaderException)
		{}
		catch (ArgumentNullException)
		{}

		return config;
	}
}
}