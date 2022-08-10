using System;
using System.Collections.Generic;
using System.Reflection;
using Syy1125.OberthEffect.Spec.ModLoading;
using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.SchemaGen
{
public class SchemaGenerator
{
	public static Dictionary<string, object> GenerateTopLevelSchema(Type type)
	{
		var schema = GenerateSchemaObject(type);
		schema.Add("$id", type.GetCustomAttribute<CreateSchemaFileAttribute>().FileName);
		return schema;
	}

	public static Dictionary<string, object> GenerateSchemaObject(Type type)
	{
		// Primitive types
		if (type == typeof(string))
		{
			return new() { { "type", "string" } };
		}
		else if (type == typeof(int))
		{
			return new() { { "type", "integer" } };
		}
		else if (type == typeof(float) || type == typeof(double))
		{
			return new() { { "type", "number" } };
		}
		else if (type == typeof(bool))
		{
			return new() { { "type", "boolean" } };
		}
		// Custom serialization types
		else if (type == typeof(Vector2))
		{
			return new()
			{
				{ "type", "object" },
				{
					"properties",
					new Dictionary<string, object>
					{
						{ "X", new Dictionary<string, object> { { "type", "number" } } },
						{ "Y", new Dictionary<string, object> { { "type", "number" } } }
					}
				}
			};
		}
		else if (type == typeof(Vector2Int))
		{
			return new()
			{
				{ "type", "object" },
				{
					"properties",
					new Dictionary<string, object>
					{
						{ "X", new Dictionary<string, object> { { "type", "integer" } } },
						{ "Y", new Dictionary<string, object> { { "type", "integer" } } }
					}
				}
			};
		}
		// Type defines its own schema
		else if (type.IsDefined(typeof(CustomSchemaGenerationAttribute)))
		{
			var schemaMethod = type.GetMethod("GenerateSchemaObject", BindingFlags.Public | BindingFlags.Static);

			try
			{
				return (Dictionary<string, object>) schemaMethod.Invoke(null, new object[] {});
			}
			catch (Exception ex) when (ex is NullReferenceException or InvalidCastException)
			{
				return new();
			}
		}
		// Array, dictionary, enum special cases
		else if (type.IsArray)
		{
			return new() { { "type", "array" }, { "items", GenerateSchemaMember(type.GetElementType()) } };
		}
		else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
		{
			return new()
			{
				{ "type", "object" },
				{
					"patternProperties", new Dictionary<string, object>
					{
						{ ".*", GenerateSchemaMember(type.GetGenericArguments()[1]) }
					}
				}
			};
		}
		else if (type.IsEnum)
		{
			return new() { { "enum", type.GetEnumNames() } };
		}
		else // Recursive object type
		{
			return GenerateSchemaObjectFromMembers(type);
		}
	}

	public static Dictionary<string, object> GenerateSchemaObjectFromMembers(Type type)
	{
		var properties = new Dictionary<string, object>();
		var required = new List<string>();

		foreach (MemberInfo member in type.GetMembers())
		{
			if (member.IsDefined(typeof(HideInSchemaAttribute))) continue;

			Dictionary<string, object> memberSchema = null;

			switch (member.MemberType)
			{
				case MemberTypes.Field:
					memberSchema = GenerateSchemaMember(((FieldInfo) member).FieldType);
					break;
				case MemberTypes.Property:
					memberSchema = GenerateSchemaMember(((PropertyInfo) member).PropertyType);
					break;
			}

			if (memberSchema == null) continue;

			ApplyAttributes(memberSchema, member);
			properties.Add(member.Name, memberSchema);

			// Because of YAML deep merging, a non-null field does not have to be present in every spec document
			// However, ID field should always be present.
			if (member.IsDefined(typeof(IdFieldAttribute)))
			{
				required.Add(member.Name);
			}
		}

		var schema = new Dictionary<string, object> { { "type", "object" }, { "properties", properties } };
		if (required.Count > 0) schema.Add("required", required);
		return schema;
	}

	public static Dictionary<string, object> GenerateSchemaMember(Type type)
	{
		var schemaFileAttribute = type.GetCustomAttribute<CreateSchemaFileAttribute>();

		if (schemaFileAttribute != null)
		{
			return new() { { "$ref", $"{schemaFileAttribute.FileName}.json" } };
		}
		else
		{
			return GenerateSchemaObject(type);
		}
	}

	private static void ApplyAttributes(Dictionary<string, object> schema, MemberInfo member)
	{
		foreach (var attribute in member.GetCustomAttributes())
		{
			if (attribute is ISchemaFieldAttribute schemaFieldAttribute)
			{
				schemaFieldAttribute.AugmentSchema(schema);
			}
		}
	}
}
}