using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks.Propulsion;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Editor.PropertyDrawers;
using Syy1125.OberthEffect.Input;
using Syy1125.OberthEffect.Lib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Simulation.Construct
{
[RequireComponent(typeof(Rigidbody2D))]
public class VehicleThrusterControl : MonoBehaviourPun,
	IPunObservable, IPropulsionBlockRegistry, IVehicleDeathListener
{
	#region Unity Fields

	[Header("Input")]
	public InputActionReference LookAction;
	public InputActionReference MoveAction;
	public InputActionReference StrafeAction;

	[Header("PID")]
	public PidConfig RotationPidConfig;

	[Header("Config")]
	public float InertiaDampenerStrength;

	#endregion

	private readonly List<IPropulsionBlock> _propulsionBlocks = new List<IPropulsionBlock>();

	private Camera _mainCamera;
	private Rigidbody2D _body;

	private Pid<float> _rotationPid;

	[ReadOnlyField]
	public InputCommand HorizontalCommand;
	[ReadOnlyField]
	public InputCommand VerticalCommand;
	[ReadOnlyField]
	public InputCommand RotateCommand;

	#region Unity Lifecycle

	private void Awake()
	{
		_mainCamera = Camera.main;
		_body = GetComponent<Rigidbody2D>();
		_rotationPid = new RotationPid(RotationPidConfig);
	}

	private void OnEnable()
	{
		PlayerControlConfig.Instance.ControlModeChanged.AddListener(OnControlModeChanged);
	}

	private void Start()
	{
		StartCoroutine(LateFixedUpdate());
	}

	private void OnDisable()
	{
		if (PlayerControlConfig.Instance != null)
		{
			PlayerControlConfig.Instance.ControlModeChanged.RemoveListener(OnControlModeChanged);
		}
	}

	#endregion

	public void OnVehicleDeath()
	{
		ClearCommands();
		SendCommands();

		enabled = false;
	}

	private void ClearCommands()
	{
		HorizontalCommand = default;
		VerticalCommand = default;
		RotateCommand = default;
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

	private IEnumerator LateFixedUpdate()
	{
		yield return new WaitForFixedUpdate();

		while (enabled)
		{
			if (photonView.IsMine)
			{
				if (ActionMapControl.Instance.IsActionMapEnabled("Player"))
				{
					switch (PlayerControlConfig.Instance.ControlMode)
					{
						case VehicleControlMode.Mouse:
							UpdateMouseModeCommands();
							break;
						case VehicleControlMode.Relative:
							UpdateRelativeModeCommands();
							break;
						case VehicleControlMode.Cruise:
							UpdateCruiseModeCommands();
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}

					if (PlayerControlConfig.Instance.InertiaDampenerActive)
					{
						ApplyInertiaDampener();
					}

					NetAndClampCommands();
				}
				else
				{
					ClearCommands();
				}
			}

			SendCommands();

			yield return new WaitForFixedUpdate();
		}
	}

	private void UpdateMouseModeCommands()
	{
		var move = MoveAction.action.ReadValue<Vector2>();
		move.x += StrafeAction.action.ReadValue<float>();

		Vector2 mousePosition = _mainCamera.ScreenToWorldPoint(LookAction.action.ReadValue<Vector2>());
		Vector2 vehiclePosition = _body.worldCenterOfMass;
		Quaternion mouseRelative = Quaternion.LookRotation(Vector3.forward, mousePosition - vehiclePosition);

		Vector2 playerTranslate = transform.InverseTransformDirection(mouseRelative * move);
		// If move signal is strong, assume that the player wants to accelerate as quickly as possible - maximize propulsion commands.
		if (move.sqrMagnitude >= 1)
		{
			float scale = 1f / Mathf.Max(Mathf.Abs(playerTranslate.x), Mathf.Abs(playerTranslate.y));
			playerTranslate *= scale;
		}

		float angle = Vector2.SignedAngle(mousePosition - vehiclePosition, transform.up);

		_rotationPid.Update(angle, Time.fixedDeltaTime);

		float playerRotate = _rotationPid.Output * Mathf.Deg2Rad;

		HorizontalCommand.PlayerValue = playerTranslate.x;
		VerticalCommand.PlayerValue = playerTranslate.y;
		RotateCommand.PlayerValue = playerRotate;
		RotateCommand.AutoValue = 0f;
	}

	private void UpdateRelativeModeCommands()
	{
		Vector2 playerTranslate = MoveAction.action.ReadValue<Vector2>();
		playerTranslate.x += StrafeAction.action.ReadValue<float>();

		Vector2 mousePosition = _mainCamera.ScreenToWorldPoint(LookAction.action.ReadValue<Vector2>());
		Vector2 vehiclePosition = _body.worldCenterOfMass;
		float angle = Vector2.SignedAngle(mousePosition - vehiclePosition, transform.up);

		_rotationPid.Update(angle, Time.fixedDeltaTime);

		float playerRotate = _rotationPid.Output * Mathf.Deg2Rad;

		HorizontalCommand.PlayerValue = playerTranslate.x;
		VerticalCommand.PlayerValue = playerTranslate.y;
		RotateCommand.PlayerValue = playerRotate;
		RotateCommand.AutoValue = 0f;
	}

	private void UpdateCruiseModeCommands()
	{
		var move = MoveAction.action.ReadValue<Vector2>();
		var strafe = StrafeAction.action.ReadValue<float>();

		HorizontalCommand.PlayerValue = strafe;
		VerticalCommand.PlayerValue = move.y;

		if (Mathf.Abs(move.x) > Mathf.Epsilon)
		{
			RotateCommand.PlayerValue = move.x;
			RotateCommand.AutoValue = 0f;
		}
		else if (Mathf.Abs(_body.angularVelocity) > Mathf.Epsilon)
		{
			RotateCommand.PlayerValue = 0f;
			RotateCommand.AutoValue = _body.angularVelocity * RotationPidConfig.DerivativeTime;
		}
		else
		{
			RotateCommand.PlayerValue = 0f;
			RotateCommand.AutoValue = 0f;
		}
	}

	private void ApplyInertiaDampener()
	{
		Vector2 localVelocity = transform.InverseTransformVector(_body.velocity);

		HorizontalCommand.AutoValue = Mathf.Approximately(HorizontalCommand.PlayerValue, 0)
			? -localVelocity.x * InertiaDampenerStrength
			: 0f;

		VerticalCommand.AutoValue = Mathf.Approximately(VerticalCommand.PlayerValue, 0)
			? -localVelocity.y * InertiaDampenerStrength
			: 0f;
	}

	private void NetAndClampCommands()
	{
		NetAndClamp(ref HorizontalCommand);
		NetAndClamp(ref VerticalCommand);
		NetAndClamp(ref RotateCommand);
	}

	private static void NetAndClamp(ref InputCommand command)
	{
		command.NetValue = Mathf.Clamp(command.PlayerValue + command.AutoValue, -1f, 1f);
		command.PlayerValue = Mathf.Clamp(command.PlayerValue, -1f, 1f);
		command.AutoValue = Mathf.Clamp(command.AutoValue, -1f, 1f);
	}

	private void SendCommands()
	{
		foreach (IPropulsionBlock block in _propulsionBlocks)
		{
			block.SetPropulsionCommands(HorizontalCommand, VerticalCommand, RotateCommand);
		}
	}

	#endregion

	#region PUN

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			SendCommandStream(stream, HorizontalCommand);
			SendCommandStream(stream, VerticalCommand);
			SendCommandStream(stream, RotateCommand);
		}
		else
		{
			ReadCommandStream(stream, ref HorizontalCommand);
			ReadCommandStream(stream, ref VerticalCommand);
			ReadCommandStream(stream, ref RotateCommand);
		}
	}

	private static void SendCommandStream(PhotonStream stream, InputCommand command)
	{
		stream.SendNext(command.PlayerValue);
		stream.SendNext(command.AutoValue);
		stream.SendNext(command.NetValue);
	}

	private static void ReadCommandStream(PhotonStream stream, ref InputCommand command)
	{
		command.PlayerValue = (float) stream.ReceiveNext();
		command.AutoValue = (float) stream.ReceiveNext();
		command.NetValue = (float) stream.ReceiveNext();
	}

	#endregion

	private void OnControlModeChanged()
	{
		_rotationPid.Reset();
	}
}
}