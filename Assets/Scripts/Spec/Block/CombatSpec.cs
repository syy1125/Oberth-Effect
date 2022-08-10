using Syy1125.OberthEffect.Spec.Validation.Attributes;

namespace Syy1125.OberthEffect.Spec.Block
{
public struct CombatSpec
{
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxHealth;
	[ValidateArmorTypeId]
	public string ArmorTypeId;
	public float IntegrityScore;
}
}