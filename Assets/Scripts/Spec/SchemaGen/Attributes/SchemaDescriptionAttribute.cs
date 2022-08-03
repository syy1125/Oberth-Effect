using System;
using System.Collections.Generic;

namespace Syy1125.OberthEffect.Spec.SchemaGen.Attributes
{
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class SchemaDescriptionAttribute : Attribute, ISchemaFieldAttribute
{
	private readonly string _description;

	public SchemaDescriptionAttribute(string description)
	{
		_description = description;
	}

	public void AugmentSchema(Dictionary<string, object> schema)
	{
		schema.Add("description", _description);
	}
}
}