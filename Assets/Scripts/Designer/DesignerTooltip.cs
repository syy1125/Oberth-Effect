using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Components.UserInterface;
using Syy1125.OberthEffect.Designer.Palette;
using UnityEngine;

namespace Syy1125.OberthEffect.Designer
{
public class DesignerTooltip : MonoBehaviour
{
	[Header("References")]
	public VehicleDesigner Designer;
	public VehicleBuilder Builder;
	public DesignerPaletteUse PaletteUse;
	public DesignerGridMove GridMove;
	public DesignerAreaMask AreaMask;

	[Header("Config")]
	public float BlockTooltipDelay = 0.5f;

	private Vector2Int? _tooltipPosition;

	private void Update()
	{
		Vector2Int? tooltipPosition = null;

		if (AreaMask.Hovering && PaletteUse.CurrentSelection is CursorSelection && !GridMove.Dragging)
		{
			tooltipPosition = Designer.HoverPositionInt;
		}

		if (tooltipPosition != _tooltipPosition)
		{
			if (_tooltipPosition != null)
			{
				CancelInvoke(nameof(ShowBlockTooltip));
				HideTooltip();
			}

			if (tooltipPosition != null)
			{
				Invoke(nameof(ShowBlockTooltip), BlockTooltipDelay);
			}

			_tooltipPosition = tooltipPosition;
		}
	}

	private void ShowBlockTooltip()
	{
		if (!isActiveAndEnabled) return;
		if (_tooltipPosition == null || TooltipControl.Instance == null) return;
		GameObject go = Builder.GetBlockObjectAt(_tooltipPosition.Value);
		if (go == null) return;

		TooltipControl.Instance.SetTooltip(TooltipProviderUtils.CombineTooltips(go));
	}

	private void HideTooltip()
	{
		if (TooltipControl.Instance == null) return;

		TooltipControl.Instance.SetTooltip(null);
	}
}
}