using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Common.UserInterface
{
public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public float Delay = 0.2f;
	[TextArea]
	public string TooltipText;

	private float _enterTime;
	private float _exitTime;

	public void OnPointerEnter(PointerEventData eventData)
	{
		_enterTime = Time.unscaledTime;
		Invoke(nameof(ApplyTooltip), Delay);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		_exitTime = Time.unscaledTime;
		CancelInvoke(nameof(ApplyTooltip));

		if (TooltipControl.Instance != null)
		{
			TooltipControl.Instance.SetTooltip(null);
		}
	}

	private void ApplyTooltip()
	{
		if (TooltipControl.Instance != null)
		{
			TooltipControl.Instance.SetTooltip(TooltipText);
		}
	}
}
}