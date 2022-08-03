using System;

namespace Syy1125.OberthEffect.Spec.SchemaGen.Attributes
{
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class CreateSchemaFileAttribute : Attribute
{
	public readonly string FileName;

	public CreateSchemaFileAttribute(string fileName)
	{
		FileName = fileName;
	}
}
}