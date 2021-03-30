using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class VehicleControl : MonoBehaviour
{
	public InputActionReference Move;
	public float Acceleration;
	public float FuelRecovery;

	public Image FuelGauge;

	private float _fuel;
	private Rigidbody2D _body;
	private Camera _mainCamera;

	private void Awake()
	{
		_fuel = 1;
		_body = GetComponent<Rigidbody2D>();
		_mainCamera = Camera.main;
	}

	private void FixedUpdate()
	{
		Vector2 mousePosition = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
		Vector2 vehiclePosition = transform.position;
		float angle = Vector3.SignedAngle(mousePosition - vehiclePosition, Vector3.up, Vector3.back);
		transform.rotation = Quaternion.Euler(0, 0, angle);

		_fuel = Mathf.Min(_fuel + Time.fixedDeltaTime * FuelRecovery, 1f);

		if (_fuel > 0)
		{
			var input = Move.action.ReadValue<Vector2>();
			_body.AddRelativeForce(input * Acceleration);
			_fuel -= (input.x + input.y) * Time.fixedDeltaTime;
		}
	}

	private void Update()
	{
		FuelGauge.fillAmount = Mathf.Clamp01(_fuel);
	}
}