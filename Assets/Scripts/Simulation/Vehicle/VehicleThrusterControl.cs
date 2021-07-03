using System;
using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks.Propulsion;
using Syy1125.OberthEffect.Common;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Simulation.Vehicle
{
[RequireComponent(typeof(Rigidbody2D))]
public class VehicleThrusterControl : MonoBehaviourPun, IPunObservable, IPropulsionBlockRegistry,
	IPunInstantiateMagicCallback
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

	public VehicleControlMode ControlMode { get; private set; }
	public UnityEvent ControlModeChanged;

	#endregion

	private List<IPropulsionBlock> _propulsionBlocks;

	private Camera _mainCamera;
	private Rigidbody2D _body;

	private LinkedList<float> _angleHistory;
	private float _integral;

	public float ForwardBackCommand { get; private set; }
	public float StrafeCommand { get; private set; }
	public float RotateCommand { get; private set; }

	#region Unity Lifecycle

	private void Awake()
	{
		_propulsionBlocks = new List<IPropulsionBlock>();

		_mainCamera = Camera.main;
		_body = GetComponent<Rigidbody2D>();
		_angleHistory = new LinkedList<float>();
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

	#endregion

	#region Propulsion Registry

	public void RegisterBlock(IPropulsionBlock block)
	{
		_propulsionBlocks.Add(block);
	}

	public void UnregisterBlock(IPropulsionBlock block)
	{
		bool success = _propulsionBlocks.Remove(block);
		if (!success)
		{
			Debug.LogError($"Failed to remove propulsion block {block}");
		}
	}

	#endregion

	#region Update

	private void FixedUpdate()
	{
		if (photonView.IsMine)
		{
			switch (ControlMode)
			{
				case VehicleControlMode.Mouse:
					UpdateMouseModeCommands();
					break;
				case VehicleControlMode.Cruise:
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

		SendCommands();
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

	private void SendCommands()
	{
		foreach (IPropulsionBlock block in _propulsionBlocks)
		{
			block.SetPropulsionCommands(ForwardBackCommand, StrafeCommand, RotateCommand);
		}
	}

	#endregion

	#region PUN

	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		VehicleBlueprint blueprint =
			JsonUtility.FromJson<VehicleBlueprint>((string) info.photonView.InstantiationData[0]);

		ControlMode = blueprint.DefaultControlMode;
		ControlModeChanged.Invoke();
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(ForwardBackCommand);
			stream.SendNext(StrafeCommand);
			stream.SendNext(RotateCommand);
		}
		else
		{
			ForwardBackCommand = (float) stream.ReceiveNext();
			StrafeCommand = (float) stream.ReceiveNext();
			RotateCommand = (float) stream.ReceiveNext();
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
			VehicleControlMode.Mouse => VehicleControlMode.Cruise,
			VehicleControlMode.Cruise => VehicleControlMode.Mouse,
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