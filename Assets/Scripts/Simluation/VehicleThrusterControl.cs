using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class VehicleThrusterControl : MonoBehaviour
{
	public InputActionReference Move;
	public InputActionReference Strafe;

	public float ForwardBackCommand { get; private set; }
	public float StrafeCommand { get; private set; }
	public float RotateCommand { get; private set; }

	private void FixedUpdate()
	{
		var move = Move.action.ReadValue<Vector2>();
		var strafe = Strafe.action.ReadValue<float>();

		ForwardBackCommand = move.y;
		StrafeCommand = Mathf.Clamp(move.x + strafe, -1f, 1f);
	}
}