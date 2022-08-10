using Syy1125.OberthEffect.Spec.Validation.Attributes;

namespace Syy1125.OberthEffect.Spec.Block.Weapon
{
public class PointDefenseTargetSpec
{
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxHealth;
	[ValidateArmorTypeId]
	public string ArmorTypeId;
}
}