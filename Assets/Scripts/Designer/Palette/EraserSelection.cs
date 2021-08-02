using UnityEngine;

namespace Syy1125.OberthEffect.Designer.Palette
{
public class EraserSelection : IPaletteSelection
{
	public DesignerCursorTexture.CursorStatus TargetCursorStatus => DesignerCursorTexture.CursorStatus.Eraser;

	private static EraserSelection _instance;
	public static EraserSelection Instance => _instance ??= new EraserSelection();

	private EraserSelection()
	{}

	public bool Equals(IPaletteSelection other)
	{
		// Singleton behaviour
		return ReferenceEquals(this, other);
	}
}
}