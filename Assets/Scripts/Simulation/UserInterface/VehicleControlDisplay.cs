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
				_thrusterControl.InertiaDampenerChanged.RemoveListener(UpdateDisplay);
			}

			_thrusterControl = value;

			if (_thrusterControl != null)
			{
				_thrusterControl.InertiaDampenerChanged.AddListener(UpdateDisplay);
			}
		}
	}

	public Text InertiaDampenerDisplay;

	private void OnEnable()
	{
		if (_thrusterControl != null)
		{
			_thrusterControl.InertiaDampenerChanged.AddListener(UpdateDisplay);
		}
	}

	private void OnDisable()
	{
		if (_thrusterControl != null)
		{
			_thrusterControl.InertiaDampenerChanged.RemoveListener(UpdateDisplay);
		}
	}

	private void UpdateDisplay()
	{
		string inertiaDampenerStatus = ThrusterControl.InertiaDampenerActive
			? "<color=\"green\">ON</color>"
			: "<color=\"red\">OFF</color>";
		InertiaDampenerDisplay.text = $"Inertia Dampener {inertiaDampenerStatus}";
	}
}
}