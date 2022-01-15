using Syy1125.OberthEffect.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Prototyping
{
public class ScreenShakeTest : MonoBehaviour
{
	private CameraScreenShake _screenShake;
	public InputAction Linear;
	public InputAction Quadratic;
	public InputAction Cubic;

	private void Awake()
	{
		_screenShake = GetComponent<CameraScreenShake>();
	}

	private void Start()
	{
		Linear.Enable();
		Quadratic.Enable();
		Cubic.Enable();
	}

	private void Update()
	{
		if (Linear.triggered)
		{
			_screenShake.AddInstance(1f, 0.5f, CameraScreenShake.DecayCurve.Linear);
		}

		if (Quadratic.triggered)
		{
			_screenShake.AddInstance(1f, 0.5f, CameraScreenShake.DecayCurve.Quadratic);
		}

		if (Cubic.triggered)
		{
			_screenShake.AddInstance(1f, 0.5f, CameraScreenShake.DecayCurve.Cubic);
		}
	}
}
}