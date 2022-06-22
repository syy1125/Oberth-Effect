using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.CoreMod
{
public class VolatileSpec
{
	// Information only - up to the exact components of the block to control explosion behaviour when the block is actually destroyed.
	[RequireChecksumLevel(ChecksumLevel.Everything)]
	public bool AlwaysExplode;
	public ControlConditionSpec ActivationCondition;
	public Vector2 ExplosionOffset;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxRadius;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxDamage;
}
}