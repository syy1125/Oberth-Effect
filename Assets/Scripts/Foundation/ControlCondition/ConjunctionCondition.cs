using System;
using System.Collections.Generic;
using System.Linq;

namespace Syy1125.OberthEffect.Foundation.ControlCondition
{
public class ConjunctionCondition : IControlCondition
{
	private IControlCondition[] _conditions;

	public ConjunctionCondition(IControlCondition[] conditions)
	{
		_conditions = conditions;
	}

	public bool IsTrue(Func<string, string> getState)
	{
		return _conditions.All(condition => condition.IsTrue(getState));
	}

	public IEnumerable<string> GetControlGroups()
	{
		return _conditions.SelectMany(condition => condition.GetControlGroups());
	}
}
}