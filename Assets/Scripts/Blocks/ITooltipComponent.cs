using System.Text;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
public interface ITooltipComponent
{
	/// <summary>
	/// Export tooltip to the given StringBuilder, with the specified indent.
	/// The exported tooltip is expected to have an extra newline at the end.
	/// </summary>
	/// <returns>True if the tooltip has been modified.</returns>
	bool GetTooltip(StringBuilder builder, string indent);
}

public static class TooltipProviderUtils
{
	public static string CombineTooltips(GameObject block)
	{
		StringBuilder tooltip = new();

		foreach (MonoBehaviour behaviour in block.GetComponents<MonoBehaviour>())
		{
			if (behaviour is ITooltipComponent provider && provider.GetTooltip(tooltip, ""))
			{
				tooltip.AppendLine();
			}
		}

		return tooltip.ToString().TrimEnd();
	}
}
}