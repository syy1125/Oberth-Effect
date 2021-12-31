using System;
using System.Collections.Generic;
using System.Linq;

namespace Syy1125.OberthEffect.Common.ControlCondition
{
public class DisjunctionCondition : IControlCondition
{
	private IControlCondition[] _conditions;

	public DisjunctionCondition(IControlCondition[] conditions)
	{
		_conditions = conditions;
	}

	public bool IsTrue(Func<string, string> getState)
	{
		return _conditions.Any(condition => condition.IsTrue(getState));
	}

	public IEnumerable<string> GetControlGroups()
	{
		return _conditions.SelectMany(condition => condition.GetControlGroups());
	}
}
}