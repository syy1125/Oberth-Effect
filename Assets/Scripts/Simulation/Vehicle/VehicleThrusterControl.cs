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
	public PidConfig RotationPidConfig;

	[Header("Config")]
	public float InertiaDampenerStrength;

	#endregion

	private PlayerControlConfig _controlConfig;

	private readonly List<IPropulsionBlock> _propulsionBlocks = new List<IPropulsionBlock>();

	private Camera _mainCamera;
	private Rigidbody2D _body;

	private Pid<float> _rotationPid;

	private Vector2 _translateCommand;
	private float _rotateCommand;

	#region Unity Lifecycle

	private void Awake()
	{
		_mainCamera = Camera.main;
		_body = GetComponent<Rigidbody2D>();
		_rotationPid = new Pid<float>(
			RotationPidConfig,
			(a, b) => a + b,
			(a, b) => Mathf.DeltaAngle(b, a),
			(a, b) => a * b
		);
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
	}

	private void OnDisable()
	{
		MoveAction.action.Disable();
		StrafeAction.action.Disable();

		_controlConfig.ControlModeChanged.RemoveListener(OnControlModeChanged);
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

		_rotationPid.Update(angle, Time.fixedDeltaTime);

		_rotateCommand = _rotationPid.Output * Mathf.Deg2Rad;
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
			_rotateCommand = _body.angularVelocity * RotationPidConfig.DerivativeTime;
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
		_rotationPid.Reset();
	}
}
}