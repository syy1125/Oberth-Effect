using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;

namespace Syy1125.OberthEffect.Designer.Palette
{
public class BlockSelection : IPaletteSelection
{
	public DesignerCursorTexture.CursorStatus TargetCursorStatus => DesignerCursorTexture.CursorStatus.Default;

	private readonly string _blockId;

	public BlockSelection(string blockId)
	{
		_blockId = blockId;
	}

	public void HandleClick(VehicleBuilder builder, Vector2Int position, int rotation)
	{
		builder.AddBlock(BlockDatabase.Instance.GetSpecInstance(_blockId).Spec, position, rotation);
	}

	public bool Equals(IPaletteSelection other)
	{
		if (other is BlockSelection selection)
		{
			return _blockId == selection._blockId;
		}
		else
		{
			return false;
		}
	}
}
}