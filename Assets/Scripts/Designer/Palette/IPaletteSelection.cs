using System;
using UnityEngine;

namespace Syy1125.OberthEffect.Designer.Palette
{
public interface IPaletteSelection : IEquatable<IPaletteSelection>
{
	DesignerCursorTexture.CursorStatus TargetCursorStatus { get; }

	void HandleClick(VehicleBuilder builder, Vector2Int position, int rotation);
}
}