using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Designer
{
[Serializable]
public class PickColorEvent : UnityEvent<Vector2>
{}

[RequireComponent(typeof(RectTransform))]
public class ColorVisualSquare : MonoBehaviour, IPointerDownHandler, IDragHandler
{
	public RectTransform Knob;
	public PickColorEvent OnChange;

	private Camera _mainCamera;
	private RectTransform _transform;
	private Image _image;
	private Image _knobImage;

	private void Awake()
	{
		_mainCamera = Camera.main;
		_transform = GetComponent<RectTransform>();
		_image = GetComponent<Image>();
		_knobImage = Knob.GetComponent<Image>();
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		Vector2 position = GetRectPosition(eventData.position);
		OnChange.Invoke(position);
	}

	public void OnDrag(PointerEventData eventData)
	{
		Vector2 position = GetRectPosition(eventData.position);
		OnChange.Invoke(position);
	}

	private Vector2 GetRectPosition(Vector2 screenPosition)
	{
		Vector3 localPosition = transform.InverseTransformPoint(_mainCamera.ScreenToWorldPoint(screenPosition));
		Rect rect = _transform.rect;
		Vector2 rectPosition = (new Vector2(localPosition.x, localPosition.y) - rect.min) / rect.size;
		return new Vector2(Mathf.Clamp01(rectPosition.x), Mathf.Clamp01(rectPosition.y));
	}

	public void UpdateColor(Vector3 hsv)
	{
		Color fullColor = Color.HSVToRGB(hsv.x, 1f, 1f);
		_image.color = fullColor;

		var anchor = new Vector2(hsv.y, hsv.z);
		Knob.anchorMin = anchor;
		Knob.anchorMax = anchor;
		_knobImage.color = Color.HSVToRGB(hsv.x, hsv.y, hsv.z);
	}
}
}