using System.Collections.Generic;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
public interface ITooltipProvider
{
	string GetTooltip();
}

public static class TooltipProviderUtils
{
	public static string CombineTooltips(GameObject block)
	{
		LinkedList<string> tooltips = new LinkedList<string>();

		foreach (MonoBehaviour behaviour in block.GetComponents<MonoBehaviour>())
		{
			if (behaviour is ITooltipProvider provider)
			{
				string tooltip = provider.GetTooltip();

				if (!string.IsNullOrWhiteSpace(tooltip))
				{
					tooltips.AddLast(provider.GetTooltip());
				}
			}
		}

		return string.Join("\n\n", tooltips);
	}
}
}