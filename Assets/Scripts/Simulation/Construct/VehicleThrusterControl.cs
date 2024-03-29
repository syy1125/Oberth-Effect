﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Blocks.Propulsion;
using Syy1125.OberthEffect.Editor.PropertyDrawers;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Lib.Pid;
using Syy1125.OberthEffect.Simulation.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Simulation.Construct
{
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(VehicleWeaponControl))]
public class VehicleThrusterControl : MonoBehaviourPun,
	IPunObservable,
	IPropulsionBlockRegistry,
	IVehicleDeathListener
{
	#region Unity Fields

	[Header("Input")]
	public InputActionReference LookAction;
	public InputActionReference MoveAction;
	public InputActionReference StrafeAction;

	[Header("Config")]
	public float InertiaDampenerStrength;

	#endregion

	private readonly List<IPropulsionBlock> _propulsionBlocks = new();
	private float? _maxForwardThrust = null;

	private Camera _mainCamera;
	private Rigidbody2D _body;
	private VehicleCore _core;
	private VehicleWeaponControl _weaponControl;

	private class ThrusterRotationPid : IPid<float>
	{
		private struct PidHistory
		{
			public float Time;
			public float Value;
			public float Integral;
		}

		public float Output { get; private set; }

		private readonly PidConfig _config;
		private readonly float _timeLimit;

		private float _timer;
		private readonly LinkedList<PidHistory> _history;
		private float _integral;

		public ThrusterRotationPid(PidConfig config, float timeLimit)
		{
			_config = config;
			_timeLimit = timeLimit;

			_timer = 0f;
			_history = new();
			_integral = 0f;
		}

		public void Update(float value, float deltaTime)
		{
			_timer += deltaTime;

			float baseResponse = value;

			if (
				_history.Count > 0
				&& !Mathf.Approximately(_config.DerivativeTime, 0f)
				&& !Mathf.Approximately(deltaTime, 0f)
			)
			{
				float derivative = (value - _history.Last.Value.Value) / deltaTime;
				baseResponse += derivative * _config.DerivativeTime;
			}

			if (!Mathf.Approximately(_config.IntegralTime, 0f) && _timeLimit > Mathf.Epsilon)
			{
				float stepIntegral = value * deltaTime;
				_history.AddLast(new PidHistory { Time = _timer, Value = value, Integral = stepIntegral });
				_integral += stepIntegral;

				float minTime = _timer - _timeLimit;
				while (_history.Count > 0 && _history.First.Value.Time < minTime)
				{
					_integral -= _history.First.Value.Integral;
					_history.RemoveFirst();
				}

				baseResponse += _integral / _config.IntegralTime;
			}
			else if (!Mathf.Approximately(_config.DerivativeTime, 0f))
			{
				_history.Clear();
				_history.AddLast(new PidHistory { Value = value });
			}

			Output = baseResponse * _config.Response;
		}

		public void Reset()
		{
			_timer = 0f;
			_history.Clear();
			_integral = 0f;
		}
	}

	private PidConfig _pidConfig;
	private IPid<float> _rotationPid;

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
		_core = GetComponent<VehicleCore>();
		_weaponControl = GetComponent<VehicleWeaponControl>();
	}

	private void OnEnable()
	{
		PlayerControlConfig.Instance.ControlModeChanged.AddListener(OnControlModeChanged);
	}

	private void Start()
	{
		_pidConfig = _core.Blueprint.PidConfig;
		_rotationPid = new ThrusterRotationPid(_pidConfig, 2f);

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

		InvalidateCache();
	}

	public void UnregisterBlock(IPropulsionBlock block)
	{
		bool success = _propulsionBlocks.Remove(block);
		if (!success)
		{
			Debug.LogError($"Failed to remove propulsion block {block}");
		}

		InvalidateCache();
	}

	public void NotifyPropulsionBlockStateChange()
	{
		InvalidateCache();
	}

	private void InvalidateCache()
	{
		_maxForwardThrust = null;
	}

	#endregion

	#region Update

	private IEnumerator LateFixedUpdate()
	{
		while (isActiveAndEnabled)
		{
			yield return new WaitForFixedUpdate();

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

					ApplyInertiaDampener();
					NetAndClampCommands();
				}
				else
				{
					ClearCommands();
				}
			}

			SendCommands();
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
			RotateCommand.AutoValue = _body.angularVelocity * _pidConfig.DerivativeTime;
		}
		else
		{
			RotateCommand.PlayerValue = 0f;
			RotateCommand.AutoValue = 0f;
		}
	}

	private void ApplyInertiaDampener()
	{
		Vector2 localVelocity = _body.velocity;

		switch (PlayerControlConfig.Instance.InertiaDampenerMode)
		{
			case VehicleInertiaDampenerMode.Disabled:
				localVelocity = Vector2.zero;
				break;
			case VehicleInertiaDampenerMode.ParentBody:
				if (CelestialBody.CelestialBodies.Count > 0)
				{
					Vector2 parentVelocity = CelestialBody.CelestialBodies
						.Select(
							body => new
							{
								Velocity = body.GetEffectiveVelocity(SynchronizedTimer.Instance.SynchronizedTime),
								Gravity = body.GravitationalParameter
								          / (_body.worldCenterOfMass - (Vector2) body.transform.position).sqrMagnitude
							}
						)
						.Aggregate((item, acc) => item.Gravity > acc.Gravity ? item : acc)
						.Velocity;
					localVelocity -= parentVelocity;
				}

				break;
			case VehicleInertiaDampenerMode.Relative:
				if (_weaponControl == null || !_weaponControl.TargetLock)
				{
					localVelocity = Vector2.zero;
					break;
				}

				localVelocity -= _weaponControl.CurrentTarget.GetEffectiveVelocity();
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		localVelocity = transform.InverseTransformVector(localVelocity);

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

	public float GetMaxForwardThrust()
	{
		if (_maxForwardThrust == null)
		{
			_maxForwardThrust = 0f;
			foreach (
				IPropulsionBlock propulsion in _propulsionBlocks.Where(
					propulsion => propulsion.RespondToTranslation && propulsion.PropulsionActive
				)
			)
			{
				_maxForwardThrust +=
					propulsion.GetMaxPropulsionForce(
						CardinalDirectionUtils.InverseRotate(
							CardinalDirection.Up, propulsion.GetComponent<BlockCore>().Rotation
						)
					);
			}
		}

		return _maxForwardThrust.Value;
	}
}
}