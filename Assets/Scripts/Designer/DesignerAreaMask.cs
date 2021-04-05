using UnityEngine;
using UnityEngine.EventSystems;

public class DesignerAreaMask : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public VehicleDesigner Designer;

	[HideInInspector]
	public bool Hover;

	public void OnPointerEnter(PointerEventData eventData)
	{
		Hover = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		Hover = false;
	}
}