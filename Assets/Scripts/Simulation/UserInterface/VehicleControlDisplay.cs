using System;
using Syy1125.OberthEffect.Simulation.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
public class VehicleControlDisplay : MonoBehaviour
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
				DetachListeners();
			}

			_thrusterControl = value;

			if (_thrusterControl != null)
			{
				AttachListeners();
			}
		}
	}

	public Text InertiaDampenerDisplay;
	public Text ControlModeDisplay;

	private void OnEnable()
	{
		if (_thrusterControl != null)
		{
			AttachListeners();
		}
	}

	private void OnDisable()
	{
		if (_thrusterControl != null)
		{
			DetachListeners();
		}
	}

	private void AttachListeners()
	{
		_thrusterControl.InertiaDampenerChanged.AddListener(UpdateDisplay);
		_thrusterControl.ControlModeChanged.AddListener(UpdateDisplay);
	}

	private void DetachListeners()
	{
		_thrusterControl.InertiaDampenerChanged.RemoveListener(UpdateDisplay);
		_thrusterControl.ControlModeChanged.RemoveListener(UpdateDisplay);
	}

	private void UpdateDisplay()
	{
		string inertiaDampenerStatus = ThrusterControl.InertiaDampenerActive
			? "<color=\"cyan\">ON</color>"
			: "<color=\"red\">OFF</color>";
		InertiaDampenerDisplay.text = $"Inertia Dampener {inertiaDampenerStatus}";

		string controlModeStatus = ThrusterControl.ControlMode switch
		{
			ControlMode.Mouse => "<color=\"lightblue\">MOUSE</color>",
			ControlMode.Cruise => "<color=\"yellow\">CRUISE</color>",
			_ => throw new ArgumentOutOfRangeException()
		};
		ControlModeDisplay.text = $"Control Mode {controlModeStatus}";
	}
}
}