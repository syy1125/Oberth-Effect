using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Designer
{
public class DesignerAreaMask : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public bool Hovering { get; private set; }

	public void OnPointerEnter(PointerEventData eventData)
	{
		Hovering = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		Hovering = false;
	}
}
}