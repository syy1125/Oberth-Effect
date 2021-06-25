using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
public class BlockDescription : MonoBehaviour, ITooltipProvider
{
	[TextArea]
	public string Description;

	public string GetTooltip()
	{
		return Description;
	}
}
}