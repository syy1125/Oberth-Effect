using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Common.UserInterface
{
public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public float Delay = 0.2f;
	[SerializeField]
	[TextArea]
	private string TooltipText;

	private bool _tooltipShown;

	public void OnPointerEnter(PointerEventData eventData)
	{
		Invoke(nameof(ShowTooltip), Delay);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		CancelInvoke(nameof(ShowTooltip));
		HideTooltip();
	}

	public void SetTooltip(string tooltip)
	{
		TooltipText = tooltip;

		if (_tooltipShown)
		{
			ShowTooltip();
		}
	}

	private void ShowTooltip()
	{
		if (TooltipControl.Instance != null)
		{
			TooltipControl.Instance.SetTooltip(TooltipText);
			_tooltipShown = true;
		}
	}

	private void HideTooltip()
	{
		if (TooltipControl.Instance != null)
		{
			TooltipControl.Instance.SetTooltip(null);
			_tooltipShown = false;
		}
	}

	private void OnDisable()
	{
		CancelInvoke(nameof(ShowTooltip));
		if (_tooltipShown)
		{
			HideTooltip();
		}
	}
}
}