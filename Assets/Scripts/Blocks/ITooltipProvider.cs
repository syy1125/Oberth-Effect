using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks
{
public interface ITooltipProvider
{
	string GetTooltip();
}

public interface ITooltipChangeListener : IEventSystemHandler
{
	void OnTooltipChanged();
}
}