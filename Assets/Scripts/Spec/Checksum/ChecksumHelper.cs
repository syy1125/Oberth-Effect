using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Checksum
{
public static class ChecksumHelper
{
	private const BindingFlags FIELD_FLAGS = BindingFlags.Public | BindingFlags.Instance;

	public static void GetBytes(Stream stream, object target, ChecksumLevel level)
	{
		GetBytes(stream, target.GetType(), target, level);
	}

	public static void GetBytes(Stream stream, Type objectType, object value, ChecksumLevel level)
	{
		if (value == null) return;

		// Check for direct checksum computation
		if (objectType == typeof(float))
		{
			GetBytesFromPrimitive(stream, (float) value);
		}
		else if (objectType == typeof(int))
		{
			GetBytesFromPrimitive(stream, (int) value);
		}
		else if (objectType == typeof(bool))
		{
			GetBytesFromPrimitive(stream, (bool) value);
		}
		else if (objectType.IsPrimitive)
		{
			throw new ArgumentException($"Unsupported primitive type in checksum: {objectType}");
		}
		else if (typeof(string).IsAssignableFrom(objectType))
		{
			GetBytesFromString(stream, (string) value);
		}
		else if (typeof(Vector2Int).IsAssignableFrom(objectType))
		{
			GetBytesFromVector(stream, (Vector2Int) value);
		}
		else if (typeof(Vector2).IsAssignableFrom(objectType))
		{
			GetBytesFromVector(stream, (Vector2) value);
		}
		else if (typeof(ICustomChecksum).IsAssignableFrom(objectType))
		{
			((ICustomChecksum) value).GetBytes(stream, level);
		}
		else
		{
			// Special case for dictionary
			var dictionaryType = objectType.GetInterfaces()
				.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

			if (dictionaryType != null)
			{
				GetBytesFromDictionary(stream, dictionaryType, (IDictionary) value, level);
				return;
			}

			// Check for enumerable of checksum-capable objects
			var enumerableType = objectType.GetInterfaces()
				.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

			if (enumerableType != null)
			{
				GetBytesFromEnumerable(stream, enumerableType, (IEnumerable) value, level);
				return;
			}

			// No special case, do recursive checksum
			GetBytesFromFields(stream, objectType, value, level);
		}
	}

	public static void GetBytesFromFields(Stream stream, Type parentType, object parent, ChecksumLevel level)
	{
		FieldInfo[] fields = parentType.GetFields(FIELD_FLAGS);

		foreach (FieldInfo field in fields.OrderBy(field => field.Name))
		{
			CustomAttributeData levelAttribute = field.CustomAttributes.FirstOrDefault(
				attribute => attribute.AttributeType == typeof(RequireChecksumLevelAttribute)
			);

			if (levelAttribute != null && (ChecksumLevel) levelAttribute.ConstructorArguments[0].Value > level)
			{
				continue;
			}

			object fieldValue = field.GetValue(parent);
			GetBytes(stream, field.FieldType, fieldValue, level);
		}
	}

	public static void GetBytesFromPrimitive(Stream stream, float value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		stream.Write(bytes, 0, bytes.Length);
	}

	public static void GetBytesFromPrimitive(Stream stream, int value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		stream.Write(bytes, 0, bytes.Length);
	}

	public static void GetBytesFromPrimitive(Stream stream, bool value)
	{
		stream.WriteByte(Convert.ToByte(value));
	}

	public static void GetBytesFromString(Stream stream, string value)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(value);
		stream.Write(bytes, 0, bytes.Length);
	}

	public static void GetBytesFromVector(Stream stream, Vector2Int value)
	{
		GetBytesFromPrimitive(stream, value.x);
		GetBytesFromPrimitive(stream, value.y);
	}

	public static void GetBytesFromVector(Stream stream, Vector2 value)
	{
		GetBytesFromPrimitive(stream, value.x);
		GetBytesFromPrimitive(stream, value.y);
	}

	public static void GetBytesFromDictionary(Stream stream, Type dictionaryType, IDictionary dict, ChecksumLevel level)
	{
		Type keyType = dictionaryType.GetGenericArguments()[0];
		Type valueType = dictionaryType.GetGenericArguments()[1];

		if (!typeof(string).IsAssignableFrom(keyType))
		{
			Debug.LogError($"Only dictionaries with string keys are supported in checksums.");
			return;
		}

		List<Tuple<string, object>> entries = new List<Tuple<string, object>>();

		// LINQ .Cast<DictionaryEntry>() raised InvalidCastException
		var enumerator = dict.GetEnumerator();
		while (enumerator.MoveNext())
		{
			entries.Add(Tuple.Create((string) enumerator.Key, enumerator.Value));
		}

		// Impose uniform ordering to avoid issues with keys getting ordered differently on different machines.
		entries.Sort(CompareDictionaryEntries);

		foreach ((string key, object value) in entries)
		{
			GetBytes(stream, keyType, key, level);
			GetBytes(stream, valueType, value, level);
		}
	}

	private static int CompareDictionaryEntries(Tuple<string, object> left, Tuple<string, object> right)
	{
		return string.Compare(left.Item1, right.Item1, StringComparison.Ordinal);
	}

	private static void GetBytesFromEnumerable(
		Stream stream, Type enumerableType, IEnumerable value, ChecksumLevel level
	)
	{
		Type elementType = enumerableType.GetGenericArguments()[0];

		foreach (object item in value)
		{
			GetBytes(stream, elementType, item, level);
		}
	}
}
}