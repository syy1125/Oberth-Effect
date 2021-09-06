﻿using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Designer
{
public class DesignerGridMove : MonoBehaviour
{
	[Header("References")]
	public DesignerAreaMask AreaMask;

	[Header("Actions")]
	public InputActionReference PanAction;
	public InputActionReference DragAction;
	public InputActionReference ZoomAction;

	private Camera _mainCamera;

	private Vector2? _dragHandle;
	public bool Dragging => _dragHandle != null;
	private float _zoomExponent;

	private void Awake()
	{
		_mainCamera = Camera.main;
	}

	public Vector2 GetLocalMousePosition()
	{
		return transform.InverseTransformPoint(
			_mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue())
		);
	}

	private void Update()
	{
		UpdateDrag();
		UpdateZoom();
	}

	private void UpdateDrag()
	{
		bool dragging = AreaMask.Hovering && DragAction.action.ReadValue<float>() > 0.5f;

		if (dragging && _dragHandle == null)
		{
			_dragHandle = GetLocalMousePosition();
		}
		else if (!dragging && _dragHandle != null)
		{
			_dragHandle = null;
		}

		if (_dragHandle != null)
		{
			Vector2 dragHandle = _dragHandle.Value;
			Vector2 mousePosition = GetLocalMousePosition();
			// Not sure why but when using transform.Translate when zoomed out, the drag action
			// turns into a tick-length resonant oscillation and ends up causing positions of like 1e+11.
			transform.position += transform.TransformVector(mousePosition - dragHandle);
		}
		else
		{
			Vector2 pan = PanAction.action.ReadValue<Vector2>();
			transform.Translate(Time.deltaTime * -4f * pan);
		}
	}

	private void UpdateZoom()
	{
		var scroll = ZoomAction.action.ReadValue<float>();

		if (AreaMask.Hovering && !Mathf.Approximately(scroll, 0f))
		{
			Vector2 oldLocalPosition = GetLocalMousePosition();

			_zoomExponent = Mathf.Clamp(_zoomExponent + scroll / 10f, -2f, 2f);
			transform.localScale = Vector3.one * Mathf.Exp(_zoomExponent);

			Vector2 newLocalPosition = GetLocalMousePosition();
			transform.Translate(newLocalPosition - oldLocalPosition);
		}
	}
}
}