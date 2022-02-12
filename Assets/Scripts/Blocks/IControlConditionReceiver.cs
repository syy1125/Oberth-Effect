using System.Collections.Generic;
using Syy1125.OberthEffect.Foundation.ControlCondition;

namespace Syy1125.OberthEffect.Blocks
{
public interface IControlConditionProvider : IBlockRegistry<IControlConditionReceiver>
{
	bool IsConditionTrue(IControlCondition condition);

	void MarkControlGroupsActive(IEnumerable<string> controlGroupIds);
}

public interface IControlConditionReceiver
{
	void OnControlGroupsChanged(IControlConditionProvider provider);
}
}