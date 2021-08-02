using System;

namespace Syy1125.OberthEffect.Designer.Palette
{
public interface IPaletteSelection : IEquatable<IPaletteSelection>
{
	DesignerCursorTexture.CursorStatus TargetCursorStatus { get; }
}
}