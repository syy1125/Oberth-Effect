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
	public float TargetZoomExponent;
	private float _smoothZoomExponent;
	private float _smoothZoomVelocity;

	private void Awake()
	{
		_camera = GetComponent<Camera>();
		_baseSize = _camera.orthographicSize;
	}

	private void Update()
	{
		var scroll = ZoomAction.action.ReadValue<float>();

		if (Time.timeScale > Mathf.Epsilon && Mathf.Abs(scroll) > Mathf.Epsilon)
		{
			TargetZoomExponent = Mathf.Clamp(TargetZoomExponent - scroll / 10f, MinExponent, MaxExponent);
		}

		_smoothZoomExponent = Mathf.SmoothDamp(
			_smoothZoomExponent, TargetZoomExponent, ref _smoothZoomVelocity, SmoothTime,
			Mathf.Infinity, Time.unscaledDeltaTime
		);

		_camera.orthographicSize = _baseSize * Mathf.Exp(_smoothZoomExponent);
	}
}
}