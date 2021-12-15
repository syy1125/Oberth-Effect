using Syy1125.OberthEffect.Spec.ControlGroup;

namespace Syy1125.OberthEffect.Blocks
{
public interface IControlConditionProvider : IBlockRegistry<IControlConditionReceiver>
{
	bool IsConditionTrue(ControlConditionSpec condition);
}

public interface IControlConditionReceiver
{
	void OnControlGroupsChanged(IControlConditionProvider provider);
}
}