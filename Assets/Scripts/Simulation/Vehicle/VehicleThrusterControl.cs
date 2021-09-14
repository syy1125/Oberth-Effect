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
public class VehicleThrusterControl : MonoBehaviourPun,
	IPunObservable, IPunInstantiateMagicCallback, IPropulsionBlockRegistry
{
	#region Unity Fields

	[Header("Input")]
	public InputActionReference MoveAction;
	public InputActionReference StrafeAction;

	[Header("PID")]
	public float RotateResponse;
	public float RotateDerivativeTime;
	public float RotateIntegralTime;

	[Header("Config")]
	public float InertiaDampenerStrength;

	#endregion

	private PlayerControlConfig _controlConfig;

	private List<IPropulsionBlock> _propulsionBlocks;

	private Camera _mainCamera;
	private Rigidbody2D _body;

	private LinkedList<float> _angleHistory;
	private float _integral;

	private Vector2 _translateCommand;
	private float _rotateCommand;

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
	}

	public void SetPlayerControlConfig(PlayerControlConfig controlConfig)
	{
		_controlConfig = controlConfig;
		_controlConfig.ControlModeChanged.AddListener(OnControlModeChanged);
		_controlConfig.FuelPropulsionActiveChanged.AddListener(OnFuelPropulsionActiveChanged);
	}

	private void OnDisable()
	{
		MoveAction.action.Disable();
		StrafeAction.action.Disable();

		_controlConfig.ControlModeChanged.RemoveListener(OnControlModeChanged);
		_controlConfig.FuelPropulsionActiveChanged.RemoveListener(OnFuelPropulsionActiveChanged);
	}

	#endregion

	public void OnVehicleDeath()
	{
		_translateCommand = Vector2.zero;
		_rotateCommand = 0f;
		SendCommands();

		enabled = false;
	}

	#region Propulsion Registry

	public void RegisterBlock(IPropulsionBlock block)
	{
		_propulsionBlocks.Add(block);
		block.SetFuelPropulsionActive(_controlConfig.FuelPropulsionActive);
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
			switch (_controlConfig.ControlMode)
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

			if (_controlConfig.InertiaDampenerActive)
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

		_translateCommand = move + new Vector2(strafe, 0f);

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

		_rotateCommand = RotateResponse
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

		_translateCommand = new Vector2(strafe, move.y);

		if (Mathf.Abs(move.x) > Mathf.Epsilon)
		{
			_rotateCommand = move.x;
		}
		else if (Mathf.Abs(_body.angularVelocity) > Mathf.Epsilon)
		{
			_rotateCommand = _body.angularVelocity * RotateDerivativeTime;
		}
	}

	private void ApplyInertiaDampener()
	{
		Vector2 localVelocity = transform.InverseTransformVector(_body.velocity);

		if (Mathf.Approximately(_translateCommand.x, 0))
		{
			_translateCommand.x = -localVelocity.x * InertiaDampenerStrength;
		}

		if (Mathf.Approximately(_translateCommand.y, 0))
		{
			_translateCommand.y = -localVelocity.y * InertiaDampenerStrength;
		}
	}

	private void ClampCommands()
	{
		_translateCommand.x = Mathf.Clamp(_translateCommand.x, -1f, 1f);
		_translateCommand.y = Mathf.Clamp(_translateCommand.y, -1f, 1f);
		_rotateCommand = Mathf.Clamp(_rotateCommand, -1f, 1f);
	}

	private void SendCommands()
	{
		foreach (IPropulsionBlock block in _propulsionBlocks)
		{
			block.SetPropulsionCommands(_translateCommand, _rotateCommand);
		}
	}

	#endregion

	#region PUN

	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		VehicleBlueprint blueprint =
			JsonUtility.FromJson<VehicleBlueprint>((string) info.photonView.InstantiationData[0]);
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(_translateCommand);
			stream.SendNext(_rotateCommand);
		}
		else
		{
			_translateCommand = (Vector2) stream.ReceiveNext();
			_rotateCommand = (float) stream.ReceiveNext();
		}
	}

	#endregion

	private void OnControlModeChanged()
	{
		// Clear state associated with any particular control mode
		_angleHistory.Clear();
		_integral = 0f;
	}

	private void OnFuelPropulsionActiveChanged()
	{
		foreach (IPropulsionBlock block in _propulsionBlocks)
		{
			block.SetFuelPropulsionActive(_controlConfig.FuelPropulsionActive);
		}
	}
}
}