using System;

namespace Syy1125.OberthEffect.Spec.SchemaGen.Attributes
{
/// <summary>
/// Indicates that the class has a custom schema generation method. It is expected to have a public static method named GenerateSchemaObject.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class CustomSchemaGenerationAttribute : Attribute
{}
}