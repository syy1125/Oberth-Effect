using System;
using System.Collections.Generic;

namespace Syy1125.OberthEffect.Foundation.ControlCondition
{
public interface IControlCondition
{
	bool IsTrue(Func<string, string> getState);
	IEnumerable<string> GetControlGroups();
}
}