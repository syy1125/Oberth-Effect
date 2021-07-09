using System;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Simulation.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
public class VehicleInfoDisplay : MonoBehaviour
{
	[SerializeField]
	private VehicleThrusterControl _thrusterControl;

	public VehicleThrusterControl ThrusterControl
	{
		get => _thrusterControl;
		set
		{
			if (_thrusterControl != null)
			{
				DetachThrustControlListeners();
			}

			_thrusterControl = value;

			if (_thrusterControl != null)
			{
				AttachThrustControlListeners();
			}
		}
	}

	[SerializeField]
	private BlockHealthBarControl _healthBarControl;

	public BlockHealthBarControl HealthBarControl
	{
		get => _healthBarControl;
		set
		{
			if (_healthBarControl != null)
			{
				DetachHealthBarListeners();
			}

			_healthBarControl = value;

			if (_healthBarControl != null)
			{
				AttachHealthBarListeners();
			}
		}
	}

	public Text InertiaDampenerDisplay;
	public Text ControlModeDisplay;
	public Text FuelPropulsionDisplay;
	public Text HealthBarModeDisplay;

	private void OnEnable()
	{
		if (_thrusterControl != null)
		{
			AttachThrustControlListeners();
		}

		if (_healthBarControl != null)
		{
			AttachHealthBarListeners();
		}
	}

	private void Start()
	{
		UpdateDisplay();
	}

	private void OnDisable()
	{
		if (_thrusterControl != null)
		{
			DetachThrustControlListeners();
		}

		if (_healthBarControl != null)
		{
			DetachHealthBarListeners();
		}
	}

	private void AttachThrustControlListeners()
	{
		_thrusterControl.InertiaDampenerChanged.AddListener(UpdateDisplay);
		_thrusterControl.ControlModeChanged.AddListener(UpdateDisplay);
		_thrusterControl.FuelPropulsionActiveChanged.AddListener(UpdateDisplay);
	}

	private void DetachThrustControlListeners()
	{
		_thrusterControl.InertiaDampenerChanged.RemoveListener(UpdateDisplay);
		_thrusterControl.ControlModeChanged.RemoveListener(UpdateDisplay);
		_thrusterControl.FuelPropulsionActiveChanged.RemoveListener(UpdateDisplay);
	}

	private void AttachHealthBarListeners()
	{
		_healthBarControl.DisplayModeChanged.AddListener(UpdateDisplay);
	}

	private void DetachHealthBarListeners()
	{
		_healthBarControl.DisplayModeChanged.RemoveListener(UpdateDisplay);
	}

	private void UpdateDisplay()
	{
		string inertiaDampenerStatus = ThrusterControl.InertiaDampenerActive
			? "<color=\"cyan\">ON</color>"
			: "<color=\"red\">OFF</color>";
		InertiaDampenerDisplay.text = $"Inertia Dampener {inertiaDampenerStatus}";

		string controlModeStatus = ThrusterControl.ControlMode switch
		{
			VehicleControlMode.Mouse => "<color=\"lightblue\">MOUSE</color>",
			VehicleControlMode.Cruise => "<color=\"yellow\">CRUISE</color>",
			_ => throw new ArgumentOutOfRangeException()
		};
		ControlModeDisplay.text = $"Control Mode {controlModeStatus}";

		string fuelPropulsionStatus = ThrusterControl.FuelPropulsionActive
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