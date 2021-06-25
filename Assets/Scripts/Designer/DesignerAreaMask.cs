using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Designer
{
public class DesignerAreaMask : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public bool Hover { get; private set; }

	public void OnPointerEnter(PointerEventData eventData)
	{
		Hover = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		Hover = false;
	}
}
}