using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Syy1125.OberthEffect.Spec.Validation.Attributes;

namespace Syy1125.OberthEffect.Spec.Validation
{
public static class ValidationHelper
{
	private const BindingFlags FIELD_FLAGS = BindingFlags.Public | BindingFlags.Instance;

	public static List<string> ValidateRootObject(object target)
	{
		Type targetType = target.GetType();
		List<string> path = new List<string> { targetType.Name };
		List<string> errors = new List<string>();

		ValidateFields(path, targetType, target, errors);

		return errors;
	}

	public static void ValidateFields(List<string> path, object parent, List<string> errors)
	{
		ValidateFields(path, parent.GetType(), parent, errors);
	}

	private static void ValidateFields(List<string> path, Type parentType, object parent, List<string> errors)
	{
		FieldInfo[] fields = parentType.GetFields(FIELD_FLAGS);

		foreach (FieldInfo field in fields)
		{
			path.Add(field.Name);
			object fieldValue = field.GetValue(parent);
			ValidateObject(path, field.FieldType, fieldValue, field.GetCustomAttributes().ToList(), errors);
			path.RemoveAt(path.Count - 1);
		}
	}

	public static void ValidateObject(List<string> path, object value, IList<Attribute> attributes, List<string> errors)
	{
		ValidateObject(path, value.GetType(), value, attributes, errors);
	}

	private static void ValidateObject(
		List<string> path, Type valueType, object value, IList<Attribute> attributes, List<string> errors
	)
	{
		List<AbstractValidationAttribute> validators = attributes.OfType<AbstractValidationAttribute>().ToList();

		foreach (var validator in validators)
		{
			try
			{
				validator.Validate(value);
			}
			catch (ValidationError error)
			{
				errors.Add(FormatValidationError(path, error.Message));
			}
		}

		if (value == null) return;

		if (typeof(ICustomValidation).IsAssignableFrom(valueType))
		{
			// Prevent accidental altering of path
			List<string> pathCopy = new List<string>(path);
			((ICustomValidation) value).Validate(pathCopy, errors);
		}
		else
		{
			// Check for enumerable of objects
			var enumerableType = valueType.GetInterfaces()
				.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

			// Exclude string, which is enumerable as chars
			if (!typeof(string).IsAssignableFrom(valueType) && enumerableType != null)
			{
				Type elementType = enumerableType.GetGenericArguments()[0];

				path.Add("Item");

				foreach (object item in (IEnumerable) value)
				{
					ValidateObject(path, elementType, item, attributes, errors);
				}

				path.RemoveAt(path.Count - 1);

				return;
			}

			// Only recursively validate custom types
			if (valueType.Namespace != null && valueType.Namespace.StartsWith("Syy1125"))
			{
				ValidateFields(path, valueType, value, errors);
			}
		}
	}

	public static void ValidateResourceDictionary(
		List<string> path, IDictionary<string, float> dict, List<string> errors
	)
	{
		if (dict == null) return;

		var validateKey = new List<Attribute> { new ValidateVehicleResourceIdAttribute() };
		var validateValue = new List<Attribute> { new ValidateRangeFloatAttribute(0f, float.PositiveInfinity) };

		foreach (KeyValuePair<string, float> entry in dict)
		{
			path.Add("Key");
			ValidateObject(path, entry.Key, validateKey, errors);
			path.RemoveAt(path.Count - 1);
			path.Add("Value");
			ValidateObject(path, entry.Value, validateValue, errors);
			path.RemoveAt(path.Count - 1);
		}
	}

	public static string FormatValidationError(IEnumerable<string> path, string message)
	{
		return string.Join(".", path) + " " + message;
	}
}
}