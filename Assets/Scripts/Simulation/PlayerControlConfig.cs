using System;
using Syy1125.OberthEffect.Common;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Simulation
{
public class PlayerControlConfig : MonoBehaviour
{
	public InputActionReference ToggleInertiaDampenerAction;
	public InputActionReference CycleControlModeAction;
	public InputActionReference ToggleFuelPropulsionAction;


	public bool InertiaDampenerActive { get; private set; }
	public UnityEvent InertiaDampenerChanged;

	public VehicleControlMode ControlMode { get; private set; }
	public UnityEvent ControlModeChanged;

	public bool FuelPropulsionActive { get; private set; }
	public UnityEvent FuelPropulsionActiveChanged;

	private void OnEnable()
	{
		ToggleInertiaDampenerAction.action.Enable();
		ToggleInertiaDampenerAction.action.performed += ToggleInertiaDampener;
		CycleControlModeAction.action.Enable();
		CycleControlModeAction.action.performed += CycleControlMode;
		ToggleFuelPropulsionAction.action.Enable();
		ToggleFuelPropulsionAction.action.performed += ToggleFuelPropulsion;
	}

	private void Start()
	{
		ControlMode = VehicleSelection.SelectedVehicle.DefaultControlMode;
		ControlModeChanged?.Invoke();

		InertiaDampenerActive = false;
		InertiaDampenerChanged?.Invoke();
	}

	private void OnDisable()
	{
		ToggleInertiaDampenerAction.action.performed -= ToggleInertiaDampener;
		ToggleInertiaDampenerAction.action.Disable();
		CycleControlModeAction.action.performed -= CycleControlMode;
		CycleControlModeAction.action.Disable();
		ToggleFuelPropulsionAction.action.performed -= ToggleFuelPropulsion;
		ToggleFuelPropulsionAction.action.Disable();
	}

	#region Input Handlers

	private void ToggleInertiaDampener(InputAction.CallbackContext context)
	{
		InertiaDampenerActive = !InertiaDampenerActive;
		InertiaDampenerChanged?.Invoke();
	}

	private void CycleControlMode(InputAction.CallbackContext context)
	{
		ControlMode = ControlMode switch
		{
			VehicleControlMode.Mouse => VehicleControlMode.Cruise,
			VehicleControlMode.Cruise => VehicleControlMode.Mouse,
			_ => throw new ArgumentOutOfRangeException()
		};

		ControlModeChanged?.Invoke();
	}

	private void ToggleFuelPropulsion(InputAction.CallbackContext context)
	{
		FuelPropulsionActive = !FuelPropulsionActive;
		FuelPropulsionActiveChanged?.Invoke();
	}

	#endregion
}
}