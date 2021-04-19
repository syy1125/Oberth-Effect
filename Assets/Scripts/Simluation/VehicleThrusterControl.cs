using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Simluation
{
	[RequireComponent(typeof(Rigidbody2D))]
	public class VehicleThrusterControl : MonoBehaviour
	{
		[Header("Input")]
		public InputActionReference MoveAction;

		public InputActionReference StrafeAction;

		[Header("PID")]
		public float RotateResponse;

		public float RotateDerivativeTime;
		public float RotateIntegralTime;


		private Camera _mainCamera;
		private Rigidbody2D _body;

		private LinkedList<float> _angleHistory;
		private float _integral;

		public float ForwardBackCommand { get; private set; }
		public float StrafeCommand { get; private set; }
		public float RotateCommand { get; private set; }

		private void Awake()
		{
			_mainCamera = Camera.main;
			_body = GetComponent<Rigidbody2D>();
			_angleHistory = new LinkedList<float>();
		}

		private void OnEnable()
		{
			MoveAction.action.Enable();
			StrafeAction.action.Enable();
		}

		private void OnDisable()
		{
			MoveAction.action.Disable();
			StrafeAction.action.Disable();
		}

		private void FixedUpdate()
		{
			var move = MoveAction.action.ReadValue<Vector2>();
			var strafe = StrafeAction.action.ReadValue<float>();

			ForwardBackCommand = move.y;
			StrafeCommand = Mathf.Clamp(move.x + strafe, -1f, 1f);

			Vector2 mousePosition = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
			Vector2 vehiclePosition = _body.worldCenterOfMass;
			float angle = Vector2.SignedAngle(mousePosition - vehiclePosition, transform.up);

			float derivative = _angleHistory.Count > 0 ? (angle - _angleHistory.Last.Value) / Time.fixedDeltaTime : 0f;
			derivative = (derivative + 540f) % 360f - 180f;

			_integral += angle;
			_angleHistory.AddLast(angle);
			while (_angleHistory.Count > 0 && _angleHistory.Count > RotateIntegralTime / Time.fixedDeltaTime)
			{
				_integral -= _angleHistory.First.Value;
				_angleHistory.RemoveFirst();
			}

			float timeScaledIntegral = _integral * Time.fixedDeltaTime;

			RotateCommand = RotateResponse
			                * (
				                angle
				                + derivative * RotateDerivativeTime
				                + (Mathf.Abs(RotateIntegralTime) < Mathf.Epsilon
					                ? 0f
					                : timeScaledIntegral / RotateIntegralTime)
			                )
			                * Mathf.Deg2Rad;
			RotateCommand = Mathf.Clamp(RotateCommand, -1f, 1f);
		}
	}
}