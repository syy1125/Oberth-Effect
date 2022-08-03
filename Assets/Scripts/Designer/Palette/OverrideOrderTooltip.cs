using System;
using System.Collections.Generic;
using System.Text;
using Syy1125.OberthEffect.Blocks;
using UnityEngine;

namespace Syy1125.OberthEffect.Designer.Palette
{
public class OverrideOrderTooltip : MonoBehaviour, ITooltipComponent
{
	[NonSerialized]
	public IReadOnlyList<string> OverrideOrder;

	public void GetTooltip(StringBuilder builder, string indent)
	{
		builder
			.Append(indent)
			.Append("<color=\"#2222ff\">")
			.Append("Mod: ")
			.Append(string.Join(" > ", OverrideOrder))
			.Append("</color>")
			.AppendLine();
	}
}
}