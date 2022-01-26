using System.Collections.Generic;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Validation.Attributes;

namespace Syy1125.OberthEffect.Spec.Block.Propulsion
{
public class ReactionWheelSpec
{
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxTorque;
	public Dictionary<string, float> MaxResourceUse;
	public ControlConditionSpec ActivationCondition;
}
}