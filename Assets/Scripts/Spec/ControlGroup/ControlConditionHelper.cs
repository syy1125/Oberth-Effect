using System.Linq;
using Syy1125.OberthEffect.Common.ControlCondition;

namespace Syy1125.OberthEffect.Spec.ControlGroup
{
public static class ControlConditionHelper
{
	public static IControlCondition CreateControlCondition(ControlConditionSpec spec)
	{
		if (spec == null) return EmptyCondition.Instance;

		if (spec.And != null)
		{
			return new ConjunctionCondition(spec.And.Select(CreateControlCondition).ToArray());
		}
		else if (spec.Or != null)
		{
			return new DisjunctionCondition(spec.Or.Select(CreateControlCondition).ToArray());
		}
		else if (spec.Not != null)
		{
			return new NegationCondition(CreateControlCondition(spec.Not));
		}
		else
		{
			return new MatchStateCondition(spec.ControlGroupId, spec.MatchValues);
		}
	}
}
}