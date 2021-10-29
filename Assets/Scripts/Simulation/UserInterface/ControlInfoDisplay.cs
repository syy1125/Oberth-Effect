using System;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
public class ControlInfoDisplay : MonoBehaviour
{
	public BlockHealthBarControl HealthBarControl;

	public Text InertiaDampenerDisplay;
	public Text ControlModeDisplay;
	public Text HealthBarModeDisplay;

	public GameObject ControlGroupDisplayPrefab;
	public string[] ControlGroups;

	private Text[] _controlGroupDisplays;

	private void OnEnable()
	{
		CreateDisplay();
		AttachControlListeners();
		AttachHealthBarListeners();
	}

	private void Start()
	{
		UpdateDisplay();
	}

	private void OnDisable()
	{
		DetachControlListeners();
		DetachHealthBarListeners();
	}

	private void AttachControlListeners()
	{
		PlayerControlConfig.Instance.InertiaDampenerChanged.AddListener(UpdateDisplay);
		PlayerControlConfig.Instance.ControlModeChanged.AddListener(UpdateDisplay);
		PlayerControlConfig.Instance.AnyControlGroupChanged.AddListener(UpdateDisplay);
	}

	private void DetachControlListeners()
	{
		PlayerControlConfig.Instance.InertiaDampenerChanged.RemoveListener(UpdateDisplay);
		PlayerControlConfig.Instance.ControlModeChanged.RemoveListener(UpdateDisplay);
		PlayerControlConfig.Instance.AnyControlGroupChanged.RemoveListener(UpdateDisplay);
	}

	private void AttachHealthBarListeners()
	{
		HealthBarControl.DisplayModeChanged.AddListener(UpdateDisplay);
	}

	private void DetachHealthBarListeners()
	{
		HealthBarControl.DisplayModeChanged.RemoveListener(UpdateDisplay);
	}

	private void CreateDisplay()
	{
		_controlGroupDisplays = new Text[ControlGroups.Length];
		for (int i = 0; i < ControlGroups.Length; i++)
		{
			var row = Instantiate(ControlGroupDisplayPrefab, transform);
			_controlGroupDisplays[i] = row.GetComponent<Text>();
		}
	}

	private void UpdateDisplay()
	{
		string inertiaDampenerStatus = PlayerControlConfig.Instance.InertiaDampenerActive
			? "<color=\"cyan\">ON</color>"
			: "<color=\"red\">OFF</color>";
		InertiaDampenerDisplay.text = $"Inertia Dampener {inertiaDampenerStatus}";

		string controlModeStatus = PlayerControlConfig.Instance.ControlMode switch
		{
			VehicleControlMode.Mouse => "<color=\"lightblue\">MOUSE</color>",
			VehicleControlMode.Cruise => "<color=\"yellow\">CRUISE</color>",
			_ => throw new ArgumentOutOfRangeException()
		};
		ControlModeDisplay.text = $"Control Mode {controlModeStatus}";

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

		for (int i = 0; i < ControlGroups.Length; i++)
		{
			ControlGroupSpec controlGroup = ControlGroupDatabase.Instance.GetSpecInstance(ControlGroups[i]).Spec;
			ControlGroupState state = controlGroup.States[PlayerControlConfig.Instance.GetStateIndex(ControlGroups[i])];

			string stateDisplay = state.DisplayColor != null
				? $"<color=\"{state.DisplayColor}\">{state.DisplayName}</color>"
				: state.DisplayName;
			_controlGroupDisplays[i].text = $"{controlGroup.DisplayName} {stateDisplay}";
		}
	}
}
}