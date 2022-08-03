using System.Text;
using Syy1125.OberthEffect.Spec.Block;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
public class BlockDescription : MonoBehaviour, ITooltipComponent
{
	private string _description;

	public void LoadSpec(BlockSpec spec)
	{
		_description = spec.Info.Description?.Trim();
	}

	public bool GetTooltip(StringBuilder builder, string indent)
	{
		if (string.IsNullOrWhiteSpace(_description)) return false;
		
		foreach (string line in _description.Split("\n"))
		{
			builder.AppendLine($"{indent}{line}");
		}

		return true;
	}
}
}