using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Common.UserInterface
{
public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public float Delay = 0.2f;

	private float _enterTime;
	private float _exitTime;

	public void OnPointerEnter(PointerEventData eventData)
	{
		_enterTime = Time.unscaledTime;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		_exitTime = Time.unscaledTime;
	}
}
}