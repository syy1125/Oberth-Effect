using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Simulation
{
[RequireComponent(typeof(Camera))]
public class CameraZoom : MonoBehaviour
{
	public InputActionReference ZoomAction;
	public float MinExponent = -2f;
	public float MaxExponent = 3f;
	public float SmoothTime = 0.5f;

	private Camera _camera;
	private float _baseSize;
	private float _targetZoomExponent;
	private float _smoothZoomExponent;
	private float _smoothZoomVelocity;

	private void Awake()
	{
		_camera = GetComponent<Camera>();
		_baseSize = _camera.orthographicSize;
	}

	private void OnEnable()
	{
		ZoomAction.action.Enable();
	}

	private void OnDisable()
	{
		ZoomAction.action.Disable();
	}

	private void Update()
	{
		var scroll = ZoomAction.action.ReadValue<float>();

		if (Mathf.Abs(scroll) > Mathf.Epsilon)
		{
			_targetZoomExponent = Mathf.Clamp(_targetZoomExponent - scroll / 10f, MinExponent, MaxExponent);
		}

		_smoothZoomExponent = Mathf.SmoothDamp(
			_smoothZoomExponent, _targetZoomExponent, ref _smoothZoomVelocity, SmoothTime
		);

		_camera.orthographicSize = _baseSize * Mathf.Exp(_smoothZoomExponent);
	}
}
}