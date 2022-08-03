using System.Collections.Generic;

namespace Syy1125.OberthEffect.Spec.SchemaGen.Attributes
{
public interface ISchemaFieldAttribute
{
	void AugmentSchema(Dictionary<string, object> schema);
}
}