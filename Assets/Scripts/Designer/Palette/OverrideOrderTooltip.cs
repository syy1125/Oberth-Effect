using System;
using System.Collections.Generic;
using System.Text;
using Syy1125.OberthEffect.Blocks;
using UnityEngine;

namespace Syy1125.OberthEffect.Designer.Palette
{
public class OverrideOrderTooltip : MonoBehaviour, ITooltipProvider
{
	[NonSerialized]
	public IReadOnlyList<string> OverrideOrder;

	public string GetTooltip()
	{
		return new StringBuilder()
			.Append("<color=\"#8080ff\">")
			.Append("Mod: ")
			.Append(string.Join(" > ", OverrideOrder))
			.Append("</color>")
			.ToString();
	}
}
}