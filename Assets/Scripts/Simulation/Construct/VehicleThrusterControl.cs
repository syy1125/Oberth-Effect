using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks.Propulsion;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Editor.PropertyDrawers;
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
	public Vector2 TranslateCommand;
	[ReadOnlyField]
	public float RotateCommand;

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
		TranslateCommand = Vector2.zero;
		RotateCommand = 0f;
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

	private IEnumerator LateFixedUpdate()
	{
		yield return new WaitForFixedUpdate();

		while (enabled)
		{
			if (photonView.IsMine)
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

				ClampCommands();
			}

			SendCommands();

			yield return new WaitForFixedUpdate();
		}
	}

	private void UpdateMouseModeCommands()
	{
		var move = MoveAction.action.ReadValue<Vector2>();
		move.x += StrafeAction.action.ReadValue<float>();

		Vector2 mousePosition = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
		Vector2 vehiclePosition = _body.worldCenterOfMass;
		Quaternion mouseRelative = Quaternion.LookRotation(Vector3.forward, mousePosition - vehiclePosition);

		TranslateCommand = transform.InverseTransformDirection(mouseRelative * move);
		// If move signal is strong, assume that the player wants to accelerate as quickly as possible - maximize propulsion commands.
		if (move.sqrMagnitude >= 1)
		{
			float scale = 1f / Mathf.Max(Mathf.Abs(TranslateCommand.x), Mathf.Abs(TranslateCommand.y));
			TranslateCommand *= scale;
		}

		float angle = Vector2.SignedAngle(mousePosition - vehiclePosition, transform.up);

		_rotationPid.Update(angle, Time.fixedDeltaTime);

		RotateCommand = _rotationPid.Output * Mathf.Deg2Rad;
	}

	private void UpdateRelativeModeCommands()
	{
		TranslateCommand = MoveAction.action.ReadValue<Vector2>();
		TranslateCommand.x += StrafeAction.action.ReadValue<float>();

		Vector2 mousePosition = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
		Vector2 vehiclePosition = _body.worldCenterOfMass;
		float angle = Vector2.SignedAngle(mousePosition - vehiclePosition, transform.up);

		_rotationPid.Update(angle, Time.fixedDeltaTime);

		RotateCommand = _rotationPid.Output * Mathf.Deg2Rad;
	}

	private void UpdateCruiseModeCommands()
	{
		var move = MoveAction.action.ReadValue<Vector2>();
		var strafe = StrafeAction.action.ReadValue<float>();

		TranslateCommand = new Vector2(strafe, move.y);

		if (Mathf.Abs(move.x) > Mathf.Epsilon)
		{
			RotateCommand = move.x;
		}
		else if (Mathf.Abs(_body.angularVelocity) > Mathf.Epsilon)
		{
			RotateCommand = _body.angularVelocity * RotationPidConfig.DerivativeTime;
		}
	}

	private void ApplyInertiaDampener()
	{
		Vector2 localVelocity = transform.InverseTransformVector(_body.velocity);

		if (Mathf.Approximately(TranslateCommand.x, 0))
		{
			TranslateCommand.x = -localVelocity.x * InertiaDampenerStrength;
		}

		if (Mathf.Approximately(TranslateCommand.y, 0))
		{
			TranslateCommand.y = -localVelocity.y * InertiaDampenerStrength;
		}
	}

	private void ClampCommands()
	{
		TranslateCommand.x = Mathf.Clamp(TranslateCommand.x, -1f, 1f);
		TranslateCommand.y = Mathf.Clamp(TranslateCommand.y, -1f, 1f);
		RotateCommand = Mathf.Clamp(RotateCommand, -1f, 1f);
	}

	private void SendCommands()
	{
		foreach (IPropulsionBlock block in _propulsionBlocks)
		{
			block.SetPropulsionCommands(TranslateCommand, RotateCommand);
		}
	}

	#endregion

	#region PUN

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(TranslateCommand);
			stream.SendNext(RotateCommand);
		}
		else
		{
			TranslateCommand = (Vector2) stream.ReceiveNext();
			RotateCommand = (float) stream.ReceiveNext();
		}
	}

	#endregion

	private void OnControlModeChanged()
	{
		_rotationPid.Reset();
	}
}
}