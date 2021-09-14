using System;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Simulation.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
public class VehicleInfoDisplay : MonoBehaviour
{
	public PlayerControlConfig ControlConfig;
	public BlockHealthBarControl HealthBarControl;

	public Text InertiaDampenerDisplay;
	public Text ControlModeDisplay;
	public Text FuelPropulsionDisplay;
	public Text HealthBarModeDisplay;

	private void OnEnable()
	{
		AttachThrustControlListeners();
		AttachHealthBarListeners();
	}

	private void Start()
	{
		UpdateDisplay();
	}

	private void OnDisable()
	{
		DetachThrustControlListeners();
		DetachHealthBarListeners();
	}

	private void AttachThrustControlListeners()
	{
		ControlConfig.InertiaDampenerChanged.AddListener(UpdateDisplay);
		ControlConfig.ControlModeChanged.AddListener(UpdateDisplay);
		ControlConfig.FuelPropulsionActiveChanged.AddListener(UpdateDisplay);
	}

	private void DetachThrustControlListeners()
	{
		ControlConfig.InertiaDampenerChanged.RemoveListener(UpdateDisplay);
		ControlConfig.ControlModeChanged.RemoveListener(UpdateDisplay);
		ControlConfig.FuelPropulsionActiveChanged.RemoveListener(UpdateDisplay);
	}

	private void AttachHealthBarListeners()
	{
		HealthBarControl.DisplayModeChanged.AddListener(UpdateDisplay);
	}

	private void DetachHealthBarListeners()
	{
		HealthBarControl.DisplayModeChanged.RemoveListener(UpdateDisplay);
	}

	private void UpdateDisplay()
	{
		string inertiaDampenerStatus = ControlConfig.InertiaDampenerActive
			? "<color=\"cyan\">ON</color>"
			: "<color=\"red\">OFF</color>";
		InertiaDampenerDisplay.text = $"Inertia Dampener {inertiaDampenerStatus}";

		string controlModeStatus = ControlConfig.ControlMode switch
		{
			VehicleControlMode.Mouse => "<color=\"lightblue\">MOUSE</color>",
			VehicleControlMode.Cruise => "<color=\"yellow\">CRUISE</color>",
			_ => throw new ArgumentOutOfRangeException()
		};
		ControlModeDisplay.text = $"Control Mode {controlModeStatus}";

		string fuelPropulsionStatus = ControlConfig.FuelPropulsionActive
			? "<color=\"cyan\">ON</color>"
			: "<color=\"red\">OFF</color>";
		FuelPropulsionDisplay.text = $"Fuel Thrusters {fuelPropulsionStatus}";

		string healthBarStatus = HealthBarControl.DisplayMode switch
		{
			BlockHealthBarControl.HealthBarDisplayMode.Always => "<color=\"yellow\">ALWAYS</color>",
			BlockHealthBarControl.HealthBarDisplayMode.IfDamaged => "<color=\"lime\">IF DAMAGED</color>",
			BlockHealthBarControl.HealthBarDisplayMode.OnHover => "<color=\"lightblue\">ON HOVER</color>",
			BlockHealthBarControl.HealthBarDisplayMode.OnHoverIfDamaged => "<color=\"cyan\">HOVER DAMAGED</color>",
			BlockHealthBarControl.HealthBarDisplayMode.Never => "<color=\"red\">NEVER</color>",
			_ => throw new ArgumentOutOfRangeException()
		};
		HealthBarModeDisplay.text = $"Show Health Bar {healthBarStatus}";
	}
}
}