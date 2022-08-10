using System.Collections.Generic;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.ModLoading;
using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;
using Syy1125.OberthEffect.Spec.Validation.Attributes;

namespace Syy1125.OberthEffect.Spec
{
[CreateSchemaFile("ArmorTypeSpecSchema")]
public struct ArmorTypeSpec
{
	[IdField]
	public string ArmorTypeId;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public string DisplayName;
	[ValidateRangeFloat(1f, 10f)]
	public float ArmorValue;
	
	// Note that the keys can be invalid damage types, if they are added for compatibility with another mod
	[SchemaDescription("Describes the multipliers that blocks of this armor type receives from specific damage types.")]
	public Dictionary<string, float> DamageModifiers;
}
}