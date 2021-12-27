using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Checksum
{
public static class ChecksumHelper
{
	private const BindingFlags FIELD_FLAGS = BindingFlags.Public | BindingFlags.Instance;

	public static uint GetChecksum(object target, ChecksumLevel level)
	{
		return GetObjectChecksum(target.GetType(), target, level);
	}

	public static uint GetObjectChecksum(Type objectType, object value, ChecksumLevel level)
	{
		if (value == null) return 0u;

		// Check for direct checksum computation
		if (objectType == typeof(float))
		{
			return GetPrimitiveChecksum((float) value);
		}
		else if (objectType == typeof(int))
		{
			return GetPrimitiveChecksum((int) value);
		}
		else if (objectType == typeof(bool))
		{
			return GetPrimitiveChecksum((bool) value);
		}
		else if (objectType.IsPrimitive)
		{
			throw new ArgumentException($"Unsupported primitive type in checksum: {objectType}");
		}
		else if (typeof(string).IsAssignableFrom(objectType))
		{
			return ((string) value).Aggregate(0u, (sum, c) => sum + Convert.ToUInt32(c));
		}
		else if (typeof(Vector2Int).IsAssignableFrom(objectType))
		{
			Vector2Int v = (Vector2Int) value;
			return GetPrimitiveChecksum(v.x) + GetPrimitiveChecksum(v.y);
		}
		else if (typeof(Vector2).IsAssignableFrom(objectType))
		{
			Vector2 v = (Vector2) value;
			return GetPrimitiveChecksum(v.x) + GetPrimitiveChecksum(v.y);
		}
		else if (typeof(ICustomChecksum).IsAssignableFrom(objectType))
		{
			return ((ICustomChecksum) value).GetChecksum(level);
		}
		else
		{
			// Special case for dictionary
			var dictionaryType = objectType.GetInterfaces()
				.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

			if (dictionaryType != null)
			{
				return GetDictionaryChecksum(dictionaryType, value, level);
			}

			// Check for enumerable of checksum-capable objects
			var enumerableType = objectType.GetInterfaces()
				.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

			if (enumerableType != null)
			{
				return GetEnumerableChecksum(value, level, enumerableType);
			}

			// No special case, do recursive checksum
			return GetRecursiveChecksum(objectType, value, level);
		}
	}

	public static uint GetRecursiveChecksum(Type parentType, object parent, ChecksumLevel level)
	{
		uint checksum = 0u;

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
			checksum += GetObjectChecksum(field.FieldType, fieldValue, level);
		}

		return checksum;
	}

	private static uint GetPrimitiveChecksum(float value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		return (uint) (bytes[0] + bytes[1] + bytes[2] + bytes[3]);
	}

	private static uint GetPrimitiveChecksum(int value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		return (uint) (bytes[0] + bytes[1] + bytes[2] + bytes[3]);
	}

	private static uint GetPrimitiveChecksum(bool value)
	{
		return Convert.ToUInt32(value);
	}

	private static uint GetDictionaryChecksum(Type dictionaryType, object value, ChecksumLevel level)
	{
		Type keyType = dictionaryType.GetGenericArguments()[0];
		Type valueType = dictionaryType.GetGenericArguments()[1];

		var enumerator = ((IDictionary) value).GetEnumerator();

		// LINQ .Cast<DictionaryEntry>() raised InvalidCastException
		uint checksum = 0u;

		while (enumerator.MoveNext())
		{
			checksum += GetObjectChecksum(keyType, enumerator.Key, level)
			            ^ GetObjectChecksum(valueType, enumerator.Value, level);
		}

		return checksum;
	}

	private static uint GetEnumerableChecksum(object value, ChecksumLevel level, Type enumerableType)
	{
		Type elementType = enumerableType.GetGenericArguments()[0];

		return ((IEnumerable) value)
			.Cast<object>()
			.Aggregate(0u, (sum, item) => sum + GetObjectChecksum(elementType, item, level));
	}
}
}