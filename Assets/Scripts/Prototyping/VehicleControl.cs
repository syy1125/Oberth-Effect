using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class VehicleControl : MonoBehaviour
{
	public InputActionReference Move;
	public float Acceleration;

	private Rigidbody2D _body;
	private Camera _mainCamera;

	private void Awake()
	{
		_body = GetComponent<Rigidbody2D>();
		_mainCamera = Camera.main;
	}

	private void FixedUpdate()
	{
		Vector2 mousePosition = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
		Vector2 vehiclePosition = transform.position;
		float angle = Vector3.SignedAngle(mousePosition - vehiclePosition, Vector3.up, Vector3.back);
		transform.rotation = Quaternion.Euler(0, 0, angle);
		
		var input = Move.action.ReadValue<Vector2>();
		_body.AddRelativeForce(input * Acceleration);
	}
}