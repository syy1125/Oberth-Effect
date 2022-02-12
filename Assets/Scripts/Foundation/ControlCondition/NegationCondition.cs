using System;
using System.Collections.Generic;

namespace Syy1125.OberthEffect.Foundation.ControlCondition
{
public class NegationCondition : IControlCondition
{
	private IControlCondition _condition;

	public NegationCondition(IControlCondition condition)
	{
		_condition = condition;
	}

	public bool IsTrue(Func<string, string> getState)
	{
		return !_condition.IsTrue(getState);
	}

	public IEnumerable<string> GetControlGroups()
	{
		return _condition.GetControlGroups();
	}
}
}