using Syy1125.OberthEffect.Spec.Block;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
public class BlockDescription : MonoBehaviour, ITooltipProvider
{
	private string _description;

	public void LoadSpec(BlockSpec spec)
	{
		_description = spec.Info.Description;
	}

	public string GetTooltip()
	{
		return _description;
	}
}
}