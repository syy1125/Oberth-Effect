using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Simulation.Vehicle
{
public enum ControlMode
{
	Mouse,
	Cruise
}


[RequireComponent(typeof(Rigidbody2D))]
public class VehicleThrusterControl : MonoBehaviour, IResourceConsumer, IPunObservable
{
	#region Unity Fields

	[Header("Input")]
	public InputActionReference MoveAction;
	public InputActionReference StrafeAction;
	public InputActionReference InertiaDampenerAction;
	public InputActionReference CycleControlModeAction;

	[Header("PID")]
	public float RotateResponse;
	public float RotateDerivativeTime;
	public float RotateIntegralTime;

	[Header("Config")]
	public float InertiaDampenerStrength;

	#endregion

	#region Public Read State

	public bool InertiaDampenerActive { get; private set; }
	public UnityEvent InertiaDampenerChanged;

	public ControlMode ControlMode { get; private set; }
	public UnityEvent ControlModeChanged;

	#endregion

	private bool _isMine;
	private List<ThrusterResponse> _thrusterResponses;
	private Dictionary<VehicleResource, float> _resourceRequests;
	private float _resourceSatisfaction;

	private Camera _mainCamera;
	private Rigidbody2D _body;

	private LinkedList<float> _angleHistory;
	private float _integral;

	public float ForwardBackCommand { get; private set; }
	public float StrafeCommand { get; private set; }
	public float RotateCommand { get; private set; }

	private void Awake()
	{
		_thrusterResponses = new List<ThrusterResponse>();
		_resourceRequests = new Dictionary<VehicleResource, float>();

		_mainCamera = Camera.main;
		_body = GetComponent<Rigidbody2D>();
		_angleHistory = new LinkedList<float>();

		var photonView = GetComponent<PhotonView>();
		// Vehicle is mine if we're in singleplayer or if the photon view is mine.
		_isMine = photonView == null || photonView.IsMine;
	}

	private void OnEnable()
	{
		MoveAction.action.Enable();
		StrafeAction.action.Enable();

		InertiaDampenerAction.action.Enable();
		InertiaDampenerAction.action.performed += ToggleInertiaDampener;
		CycleControlModeAction.action.Enable();
		CycleControlModeAction.action.performed += CycleControlMode;
	}

	private void Start()
	{
		InertiaDampenerActive = false;
		InertiaDampenerChanged.Invoke();
		ControlMode = ControlMode.Mouse;
		ControlModeChanged.Invoke();
	}

	private void OnDisable()
	{
		MoveAction.action.Disable();
		StrafeAction.action.Disable();

		InertiaDampenerAction.action.performed -= ToggleInertiaDampener;
		InertiaDampenerAction.action.Disable();
		CycleControlModeAction.action.performed -= CycleControlMode;
		CycleControlModeAction.action.Disable();
	}

	#region Update

	private void FixedUpdate()
	{
		if (_isMine)
		{
			switch (ControlMode)
			{
				case ControlMode.Mouse:
					UpdateMouseModeCommands();
					break;
				case ControlMode.Cruise:
					UpdateCruiseModeCommands();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (InertiaDampenerActive)
			{
				ApplyInertiaDampener();
			}

			ClampCommands();
		}

		SendThrustCommands();
	}

	private void UpdateMouseModeCommands()
	{
		var move = MoveAction.action.ReadValue<Vector2>();
		var strafe = StrafeAction.action.ReadValue<float>();

		ForwardBackCommand = move.y;
		StrafeCommand = move.x + strafe;

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
	}

	private void UpdateCruiseModeCommands()
	{
		var move = MoveAction.action.ReadValue<Vector2>();
		var strafe = StrafeAction.action.ReadValue<float>();

		ForwardBackCommand = move.y;
		StrafeCommand = strafe;

		if (Mathf.Abs(move.x) > Mathf.Epsilon)
		{
			RotateCommand = move.x;
		}
		else if (Mathf.Abs(_body.angularVelocity) > Mathf.Epsilon)
		{
			RotateCommand = _body.angularVelocity;
		}
	}

	private void ApplyInertiaDampener()
	{
		Vector2 localVelocity = transform.InverseTransformVector(_body.velocity);

		if (Mathf.Approximately(ForwardBackCommand, 0))
		{
			ForwardBackCommand = -localVelocity.y * InertiaDampenerStrength;
		}

		if (Mathf.Approximately(StrafeCommand, 0))
		{
			StrafeCommand = -localVelocity.x * InertiaDampenerStrength;
		}
	}

	private void ClampCommands()
	{
		ForwardBackCommand = Mathf.Clamp(ForwardBackCommand, -1f, 1f);
		StrafeCommand = Mathf.Clamp(StrafeCommand, -1f, 1f);
		RotateCommand = Mathf.Clamp(RotateCommand, -1f, 1f);
	}

	private void SendThrustCommands()
	{
		_thrusterResponses.Clear();

		LinearThruster[] linearThrusters = GetComponentsInChildren<LinearThruster>();
		foreach (LinearThruster thruster in linearThrusters)
		{
			thruster.SetCommands(ForwardBackCommand, StrafeCommand, RotateCommand);
			_thrusterResponses.Add(thruster.GetResponse());
		}
	}

	#endregion

	#region Resources

	public int GetResourcePriority()
	{
		return 0;
	}

	public Dictionary<VehicleResource, float> GetConsumptionRateRequest()
	{
		_resourceRequests.Clear();
		DictionaryUtils.AddDictionaries(
			_thrusterResponses.Select(response => response.ResourceConsumptionRateRequest),
			_resourceRequests
		);
		return _resourceRequests;
	}

	public void SatisfyResourceRequestAtLevel(float satisfaction)
	{
		foreach (ThrusterResponse response in _thrusterResponses)
		{
			_body.AddForceAtPosition(response.Force * satisfaction, response.ForceOrigin);
		}

		_resourceSatisfaction = satisfaction;
	}

	private void LateUpdate()
	{
		foreach (LinearThruster thruster in GetComponentsInChildren<LinearThruster>())
		{
			thruster.PlayEffect(_resourceSatisfaction);
		}
	}

	#endregion

	#region PUN

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(ForwardBackCommand);
			stream.SendNext(StrafeCommand);
			stream.SendNext(RotateCommand);
			stream.SendNext(_resourceSatisfaction);
		}
		else
		{
			ForwardBackCommand = (float) stream.ReceiveNext();
			StrafeCommand = (float) stream.ReceiveNext();
			RotateCommand = (float) stream.ReceiveNext();
			_resourceSatisfaction = (float) stream.ReceiveNext();
		}
	}

	#endregion

	#region Input Handlers

	private void ToggleInertiaDampener(InputAction.CallbackContext context)
	{
		InertiaDampenerActive = !InertiaDampenerActive;
		InertiaDampenerChanged.Invoke();
	}

	private void CycleControlMode(InputAction.CallbackContext context)
	{
		ControlMode = ControlMode switch
		{
			ControlMode.Mouse => ControlMode.Cruise,
			ControlMode.Cruise => ControlMode.Mouse,
			_ => throw new ArgumentOutOfRangeException()
		};

		// Clear state associated with any particular control mode
		_angleHistory.Clear();
		_integral = 0f;

		ControlModeChanged.Invoke();
	}

	#endregion
}
}