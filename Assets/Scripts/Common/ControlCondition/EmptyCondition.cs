using System;
using System.Collections.Generic;
using System.Linq;

namespace Syy1125.OberthEffect.Common.ControlCondition
{
public class EmptyCondition : IControlCondition
{
	public static EmptyCondition Instance = new EmptyCondition();
	
	private EmptyCondition()
	{}

	public bool IsTrue(Func<string, string> getState)
	{
		return true;
	}

	public IEnumerable<string> GetControlGroups()
	{
		return Enumerable.Empty<string>();
	}
}
}