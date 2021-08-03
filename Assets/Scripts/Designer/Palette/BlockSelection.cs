using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;

namespace Syy1125.OberthEffect.Designer.Palette
{
public class BlockSelection : IPaletteSelection
{
	public DesignerCursorTexture.CursorStatus TargetCursorStatus => DesignerCursorTexture.CursorStatus.Default;

	public readonly string BlockId;
	public readonly BlockSpec BlockSpec;
	private GameObject _preview;

	public BlockSelection(string blockId)
	{
		BlockId = blockId;
		BlockSpec = BlockDatabase.Instance.GetSpecInstance(blockId).Spec;
	}

	public bool Equals(IPaletteSelection other)
	{
		if (other is BlockSelection selection)
		{
			return BlockId == selection.BlockId;
		}
		else
		{
			return false;
		}
	}
}
}