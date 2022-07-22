using Syy1125.OberthEffect.Spec.Validation.Attributes;

namespace Syy1125.OberthEffect.Spec.Block.Weapon
{
public class PointDefenseTargetSpec
{
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxHealth;
	[ValidateRangeFloat(1f, 10f)]
	public float ArmorValue = 1f;
}
}