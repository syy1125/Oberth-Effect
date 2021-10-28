using Syy1125.OberthEffect.Spec.ControlGroup;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks
{
public interface IControlConditionProvider : IBlockRegistry<IControlConditionReceiver>, IEventSystemHandler
{
	bool IsConditionTrue(ControlConditionSpec condition);
}

public interface IControlConditionReceiver
{
	void OnControlGroupsChanged(IControlConditionProvider provider);
}
}