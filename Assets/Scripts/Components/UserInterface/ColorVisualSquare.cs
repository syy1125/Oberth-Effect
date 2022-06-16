using System;
using Syy1125.OberthEffect.Lib.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Components.UserInterface
{
[Serializable]
public class PickColorEvent : UnityEvent<Vector2>
{}

[RequireComponent(typeof(RectTransform))]
public class ColorVisualSquare : MonoBehaviour, IPointerDownHandler, IDragHandler
{
	public RectTransform Knob;
	public PickColorEvent OnChange;

	private RectTransform _transform;
	private Image _image;
	private Image _knobImage;

	private void Awake()
	{
		_transform = GetComponent<RectTransform>();
		_image = GetComponent<Image>();
		_knobImage = Knob.GetComponent<Image>();
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		Vector2 position = GetRectPosition(eventData);
		OnChange.Invoke(position);
	}

	public void OnDrag(PointerEventData eventData)
	{
		Vector2 position = GetRectPosition(eventData);
		OnChange.Invoke(position);
	}

	private Vector2 GetRectPosition(PointerEventData eventData)
	{
		RectTransformUtils.ScreenPointToNormalizedPointInRectangle(
			_transform, eventData.position, eventData.pressEventCamera, out Vector2 rectPosition
		);
		return new Vector2(Mathf.Clamp01(rectPosition.x), Mathf.Clamp01(rectPosition.y));
	}

	public void UpdateColor(Vector3 hsv)
	{
		_image.color = new Color(hsv.x, 1, 1);

		var anchor = new Vector2(hsv.y, hsv.z);
		Knob.anchorMin = anchor;
		Knob.anchorMax = anchor;
		_knobImage.color = Color.HSVToRGB(hsv.x, hsv.y, hsv.z);
	}
}
}