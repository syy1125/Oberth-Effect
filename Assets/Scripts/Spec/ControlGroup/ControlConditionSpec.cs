using System.Collections.Generic;
using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;
using Syy1125.OberthEffect.Spec.Validation.Attributes;

namespace Syy1125.OberthEffect.Spec.ControlGroup
{
[CreateSchemaFile("ControlConditionSpecSchema")]
[CustomSchemaGeneration]
public class ControlConditionSpec
{
	[SchemaDescription("Indicates this condition is a conjunction of several conditions. Takes precedence over Or, Not, and MatchValues.")]
	public ControlConditionSpec[] And;
	[SchemaDescription("Indicates this condition is a disjunction of several conditions. Takes precedence over Not and MatchValues.")]
	public ControlConditionSpec[] Or;
	[SchemaDescription("Indicates this condition is a negation of another condition. Takes precedence over MatchValues.")]
	public ControlConditionSpec Not;
	[ValidateControlGroupId]
	public string ControlGroupId;
	public string[] MatchValues;

	public static Dictionary<string, object> GenerateSchemaObject()
	{
		return new()
		{
			{
				"properties",
				new Dictionary<string, object>
				{
					{
						nameof(And),
						new Dictionary<string, object>
						{
							{ "type", "array" },
							{ "items", new Dictionary<string, object> { { "$ref", "#" } } }
						}
					},
					{
						nameof(Or),
						new Dictionary<string, object>
						{
							{ "type", "array" },
							{ "items", new Dictionary<string, object> { { "$ref", "#" } } }
						}
					},
					{
						nameof(Not),
						new Dictionary<string, object> { { "$ref", "#" } }
					},
					{
						nameof(ControlGroupId),
						new Dictionary<string, object> { { "type", "string" } }
					},
					{
						nameof(MatchValues),
						new Dictionary<string, object>
						{
							{ "type", "array" },
							{ "items", new Dictionary<string, object> { { "type", "string" } } }
						}
					}
				}
			},
			{
				"oneOf",
				new Dictionary<string, object>[]
				{
					new() { { "required", new[] { nameof(And) } } },
					new() { { "required", new[] { nameof(Or) } } },
					new() { { "required", new[] { nameof(Not) } } },
					new() { { "required", new[] { nameof(ControlGroupId), nameof(MatchValues) } } }
				}
			}
		};
	}
}
}