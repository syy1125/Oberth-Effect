using System;
using System.Collections.Generic;
using System.Linq;
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

	public Text ControlNotification;
	public float NotificationFadeDelay;
	public float NotificationFadeTime;

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
		PlayerControlConfig.Instance.InertiaDampenerChanged.AddListener(NotifyInertiaDampenerChange);
		PlayerControlConfig.Instance.ControlModeChanged.AddListener(UpdateDisplay);
		PlayerControlConfig.Instance.ControlModeChanged.AddListener(NotifyControlModeChange);
		PlayerControlConfig.Instance.AnyControlGroupChanged.AddListener(UpdateDisplay);
		PlayerControlConfig.Instance.AnyControlGroupChanged.AddListener(NotifyControlGroupChange);
	}

	private void DetachControlListeners()
	{
		PlayerControlConfig.Instance.InertiaDampenerChanged.RemoveListener(UpdateDisplay);
		PlayerControlConfig.Instance.InertiaDampenerChanged.RemoveListener(NotifyInertiaDampenerChange);
		PlayerControlConfig.Instance.ControlModeChanged.RemoveListener(UpdateDisplay);
		PlayerControlConfig.Instance.ControlModeChanged.RemoveListener(NotifyControlModeChange);
		PlayerControlConfig.Instance.AnyControlGroupChanged.RemoveListener(UpdateDisplay);
		PlayerControlConfig.Instance.AnyControlGroupChanged.RemoveListener(NotifyControlGroupChange);
	}

	private void AttachHealthBarListeners()
	{
		HealthBarControl.DisplayModeChanged.AddListener(UpdateDisplay);
		HealthBarControl.DisplayModeChanged.AddListener(NotifyHealthBarStatusChange);
	}

	private void DetachHealthBarListeners()
	{
		HealthBarControl.DisplayModeChanged.RemoveListener(UpdateDisplay);
		HealthBarControl.DisplayModeChanged.RemoveListener(NotifyHealthBarStatusChange);
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

	private void UpdateDisplay(List<string> _)
	{
		UpdateDisplay();
	}

	private void UpdateDisplay()
	{
		InertiaDampenerDisplay.text = GetInertiaDampenerText();
		ControlModeDisplay.text = GetControlModeText();
		HealthBarModeDisplay.text = GetHealthBarStatusText();

		for (int i = 0; i < ControlGroups.Length; i++)
		{
			_controlGroupDisplays[i].text = GetControlGroupText(ControlGroups[i]);
		}
	}

	private string GetInertiaDampenerText()
	{
		string inertiaDampenerStatus = PlayerControlConfig.Instance.InertiaDampenerActive
			? "<color=\"cyan\">ON</color>"
			: "<color=\"red\">OFF</color>";
		return $"Inertia Dampener {inertiaDampenerStatus}";
	}

	private string GetControlModeText()
	{
		string controlModeStatus = PlayerControlConfig.Instance.ControlMode switch
		{
			VehicleControlMode.Mouse => "<color=\"lime\">MOUSE</color>",
			VehicleControlMode.Relative => "<color=\"lightblue\">RELATIVE</color>",
			VehicleControlMode.Cruise => "<color=\"yellow\">CRUISE</color>",
			_ => throw new ArgumentOutOfRangeException()
		};

		return $"Control Mode {controlModeStatus}";
	}

	private string GetHealthBarStatusText()
	{
		string healthBarStatus = HealthBarControl.DisplayMode switch
		{
			BlockHealthBarControl.HealthBarDisplayMode.Always => "<color=\"yellow\">ALWAYS</color>",
			BlockHealthBarControl.HealthBarDisplayMode.IfDamaged => "<color=\"lime\">IF DAMAGED</color>",
			BlockHealthBarControl.HealthBarDisplayMode.OnHover => "<color=\"lightblue\">ON HOVER</color>",
			BlockHealthBarControl.HealthBarDisplayMode.OnHoverIfDamaged => "<color=\"cyan\">HOVER DAMAGED</color>",
			BlockHealthBarControl.HealthBarDisplayMode.Never => "<color=\"red\">NEVER</color>",
			_ => throw new ArgumentOutOfRangeException()
		};
		return $"Show Health Bar {healthBarStatus}";
	}

	private string GetControlGroupText(string controlGroupId)
	{
		ControlGroupSpec controlGroup = ControlGroupDatabase.Instance.GetSpecInstance(controlGroupId).Spec;
		ControlGroupState state = controlGroup.States[PlayerControlConfig.Instance.GetStateIndex(controlGroupId)];

		string stateDisplay = state.DisplayColor != null
			? $"<color=\"{state.DisplayColor}\">{state.DisplayName}</color>"
			: state.DisplayName;
		return $"{controlGroup.DisplayName} {stateDisplay}";
	}

	private void NotifyInertiaDampenerChange()
	{
		SetNotification(GetInertiaDampenerText());
	}

	private void NotifyControlModeChange()
	{
		SetNotification(GetControlModeText());
	}

	private void NotifyHealthBarStatusChange()
	{
		SetNotification(GetHealthBarStatusText());
	}

	private void NotifyControlGroupChange(List<string> controlGroups)
	{
		SetNotification(
			string.Join(
				"\n",
				controlGroups.Where(groupId => ControlGroups.Contains(groupId)).Select(GetControlGroupText)
			)
		);
	}

	private void SetNotification(string notification)
	{
		ControlNotification.text = notification;
		ControlNotification.CrossFadeAlpha(1f, 0f, true);
		CancelInvoke(nameof(FadeNotification));
		Invoke(nameof(FadeNotification), NotificationFadeDelay);
	}

	private void FadeNotification()
	{
		ControlNotification.CrossFadeAlpha(0f, NotificationFadeTime, true);
	}
}
}