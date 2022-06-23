using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Designer
{
public class DesignerGridMove : MonoBehaviour
{
	[Header("References")]
	public DesignerAreaMask AreaMask;

	[Header("Actions")]
	public InputActionReference PanAction;
	public InputActionReference FastPanAction;
	public InputActionReference DragAction;
	public InputActionReference ZoomAction;
	public InputActionReference ResetAction;

	private Camera _mainCamera;

	private Vector2? _dragHandle;
	public bool Dragging => _dragHandle != null;
	private float _targetZoomExponent;
	private float _currentZoomExponent;
	private float _zoomVelocity;

	private void Awake()
	{
		_mainCamera = Camera.main;
	}

	private void Start()
	{
		Vector3 areaCenter = AreaMask.GetComponent<RectTransform>().position;
		transform.position = new Vector3(areaCenter.x, areaCenter.y, transform.position.z);
	}

	public Vector2 GetLocalMousePosition()
	{
		return transform.InverseTransformPoint(
			_mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue())
		);
	}

	private void Update()
	{
		if (ResetAction.action.triggered)
		{
			ResetGridTransform();
		}

		UpdateDrag();
		UpdateZoom();
	}

	private void ResetGridTransform()
	{
		_dragHandle = null;
		Vector3 areaCenter = AreaMask.GetComponent<RectTransform>().position;
		transform.position = new Vector3(areaCenter.x, areaCenter.y, transform.position.z);
		_targetZoomExponent = 0f;
		_currentZoomExponent = 0f;
		transform.localScale = Vector3.one;
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
			// Not sure why but when using transform.Translate or transform.localScale when zoomed out, the drag action
			// turns into a tick-length resonant oscillation and ends up causing positions of like 1e+11.
			transform.position += transform.TransformVector(mousePosition - dragHandle);
		}
		else
		{
			Vector2 pan = PanAction.action.ReadValue<Vector2>();
			if (FastPanAction.action.ReadValue<float>() > 0.5f) pan *= 4f;
			transform.Translate(Time.deltaTime * -5f * pan);
		}
	}

	private void UpdateZoom()
	{
		var scroll = ZoomAction.action.ReadValue<float>();

		if (AreaMask.Hovering && !Mathf.Approximately(scroll, 0f))
		{
			_targetZoomExponent = Mathf.Clamp(_targetZoomExponent + scroll / 10f, -2f, 1f);
		}

		float zoomExponent = Mathf.SmoothDamp(_currentZoomExponent, _targetZoomExponent, ref _zoomVelocity, 0.1f);

		if (!Mathf.Approximately(zoomExponent, _currentZoomExponent))
		{
			_currentZoomExponent = zoomExponent;
			Vector2 oldLocalPosition = GetLocalMousePosition();
			transform.localScale = Vector3.one * Mathf.Exp(_currentZoomExponent);
			Vector2 newLocalPosition = GetLocalMousePosition();
			transform.Translate(newLocalPosition - oldLocalPosition);
		}
	}
}
}