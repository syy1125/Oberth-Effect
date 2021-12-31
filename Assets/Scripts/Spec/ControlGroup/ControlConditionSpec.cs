using Syy1125.OberthEffect.Spec.Validation.Attributes;

namespace Syy1125.OberthEffect.Spec.ControlGroup
{
public class ControlConditionSpec
{
	public ControlConditionSpec[] And;
	public ControlConditionSpec[] Or;
	public ControlConditionSpec Not;
	[ValidateControlGroupId]
	public string ControlGroupId;
	public string[] MatchValues;
}
}