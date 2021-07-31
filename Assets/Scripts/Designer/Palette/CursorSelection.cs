using UnityEngine;

namespace Syy1125.OberthEffect.Designer.Palette
{
public class CursorSelection : IPaletteSelection
{
	public DesignerCursorTexture.CursorStatus TargetCursorStatus => DesignerCursorTexture.CursorStatus.Default;

	private static CursorSelection _instance;
	public static CursorSelection Instance => _instance ??= new CursorSelection();

	private CursorSelection()
	{}

	public void HandleClick(VehicleBuilder builder, Vector2Int position, int rotation)
	{
		// Clicking with a cursor does nothing
	}

	public bool Equals(IPaletteSelection other)
	{
		// Singleton behaviour
		return ReferenceEquals(this, other);
	}
}
}