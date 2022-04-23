using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Spec;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
public class ControlInfoDisplay : MonoBehaviour
{
	public BlockHealthBarControl HealthBarControl;

	public Text HudModeDisplay;
	public Text InertiaDampenerDisplay;
	public Text ControlModeDisplay;
	public Text InvertAimDisplay;
	public Text HealthBarModeDisplay;

	public Text ControlNotification;
	public float NotificationFadeDelay;
	public float NotificationFadeTime;

	public GameObject ControlGroupDisplayPrefab;

	private bool _started;
	private Coroutine _initNotification;
	private Coroutine _fadeNotification;
	private Dictionary<string, Text> _controlGroupDisplays;

	private UnityEvent OnDetachListeners;

	private void Awake()
	{
		OnDetachListeners = new UnityEvent();
	}

	private void OnEnable()
	{
		AttachControlListeners();
	}

	private void Start()
	{
		_started = true;
		UpdatePlayerControlDisplay();
		CreateControlGroupDisplay();
		UpdateControlGroupDisplay();
		_initNotification = StartCoroutine(InitNotification());
	}

	private void OnDisable()
	{
		DetachControlListeners();
	}

	private void AttachControlListeners()
	{
		LinkControlDisplay(HudModeDisplay, PlayerControlConfig.Instance.HudModeChanged, GetHudModeText);
		LinkControlDisplay(
			InertiaDampenerDisplay, PlayerControlConfig.Instance.InertiaDampenerChanged, GetInertiaDampenerText
		);
		LinkControlDisplay(ControlModeDisplay, PlayerControlConfig.Instance.ControlModeChanged, GetControlModeText);
		LinkControlDisplay(InvertAimDisplay, PlayerControlConfig.Instance.InvertAimChanged, GetInvertAimText);
		LinkControlDisplay(HealthBarModeDisplay, HealthBarControl.DisplayModeChanged, GetHealthBarStatusText);

		PlayerControlConfig.Instance.ControlGroupStateChanged.AddListener(UpdateControlGroupDisplay);
		PlayerControlConfig.Instance.ControlGroupStateChanged.AddListener(NotifyControlGroupChange);
		PlayerControlConfig.Instance.ActiveControlGroupChanged.AddListener(UpdateControlGroupDisplay);
	}

	private void DetachControlListeners()
	{
		OnDetachListeners?.Invoke();
		OnDetachListeners?.RemoveAllListeners();

		PlayerControlConfig.Instance.ControlGroupStateChanged.RemoveListener(UpdateControlGroupDisplay);
		PlayerControlConfig.Instance.ControlGroupStateChanged.RemoveListener(NotifyControlGroupChange);
		PlayerControlConfig.Instance.ActiveControlGroupChanged.RemoveListener(UpdateControlGroupDisplay);
	}

	private void LinkControlDisplay(Text display, UnityEvent changeEvent, Func<string> getText)
	{
		void ChangeHandler()
		{
			string text = getText();
			display.text = text;
			SetNotification(text);
		}

		changeEvent.AddListener(ChangeHandler);
		OnDetachListeners.AddListener(() => changeEvent.RemoveListener(ChangeHandler));
	}

	private void CreateControlGroupDisplay()
	{
		_controlGroupDisplays = new Dictionary<string, Text>();

		foreach (SpecInstance<ControlGroupSpec> instance in ControlGroupDatabase.Instance.ListControlGroups())
		{
			var row = Instantiate(ControlGroupDisplayPrefab, transform);
			_controlGroupDisplays.Add(instance.Spec.ControlGroupId, row.GetComponent<Text>());
		}
	}

	private IEnumerator InitNotification()
	{
		float delay = NotificationFadeDelay * 0.8f;
		ControlNotification.CrossFadeAlpha(1f, 0f, true);

		ControlNotification.text = GetHudModeText();
		yield return new WaitForSecondsRealtime(delay);
		ControlNotification.text = GetInertiaDampenerText();
		yield return new WaitForSecondsRealtime(delay);
		ControlNotification.text = GetControlModeText();
		yield return new WaitForSecondsRealtime(delay);
		ControlNotification.text = GetInvertAimText();
		yield return new WaitForSecondsRealtime(delay);
		ControlNotification.text = GetHealthBarStatusText();
		yield return new WaitForSecondsRealtime(delay);

		foreach (var entry in _controlGroupDisplays)
		{
			if (!PlayerControlConfig.Instance.IsControlGroupActive(entry.Key)) continue;
			ControlNotification.text = GetControlGroupText(entry.Key);
			yield return new WaitForSecondsRealtime(delay);
		}

		ControlNotification.CrossFadeAlpha(0f, NotificationFadeTime, true);
	}

	private void UpdatePlayerControlDisplay()
	{
		if (!_started) return;

		HudModeDisplay.text = GetHudModeText();
		InertiaDampenerDisplay.text = GetInertiaDampenerText();
		ControlModeDisplay.text = GetControlModeText();
		InvertAimDisplay.text = GetInvertAimText();
		HealthBarModeDisplay.text = GetHealthBarStatusText();
	}

	private void UpdateControlGroupDisplay(List<string> _)
	{
		UpdateControlGroupDisplay();
	}

	private void UpdateControlGroupDisplay()
	{
		if (!_started) return;

		foreach (var entry in _controlGroupDisplays)
		{
			entry.Value.text = GetControlGroupText(entry.Key);
			entry.Value.gameObject.SetActive(PlayerControlConfig.Instance.IsControlGroupActive(entry.Key));
		}
	}

	private string GetHudModeText()
	{
		string hudModeStatus = PlayerControlConfig.Instance.HudMode switch
		{
			HeadsUpDisplayMode.Standard => "<color=\"lime\">STANDARD</color>",
			HeadsUpDisplayMode.Extended => "<color=\"yellow\">EXTENDED</color>",
			HeadsUpDisplayMode.Minimal => "<color=\"lightblue\">MINIMAL</color>",
			_ => throw new ArgumentOutOfRangeException()
		};

		return $"HUD Level {hudModeStatus}";
	}

	private static string GetInertiaDampenerText()
	{
		string inertiaDampenerStatus = PlayerControlConfig.Instance.InertiaDampenerMode switch
		{
			VehicleInertiaDampenerMode.Disabled => "<color=\"yellow\">DISABLED</color>",
			VehicleInertiaDampenerMode.ParentBody => "<color=\"lime\">PARENT BODY</color>",
			VehicleInertiaDampenerMode.Relative => "<color=\"lightblue\">RELATIVE</color>",
			_ => throw new ArgumentOutOfRangeException()
		};

		return $"Inertia Dampener {inertiaDampenerStatus}";
	}

	private static string GetControlModeText()
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

	private static string GetInvertAimText()
	{
		string aimPointStatus = PlayerControlConfig.Instance.InvertAim
			? "<color=\"red\">INVERTED</color>"
			: "<color=\"cyan\">STANDARD</color>";
		return $"Aim Point {aimPointStatus}";
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

	private void NotifyHealthBarStatusChange()
	{
		SetNotification(GetHealthBarStatusText());
	}

	private void NotifyControlGroupChange(List<string> controlGroups)
	{
		SetNotification(
			string.Join(
				"\n",
				controlGroups.Select(GetControlGroupText)
			)
		);
	}

	private void SetNotification(string notification)
	{
		if (!_started) return;

		if (_initNotification != null)
		{
			StopCoroutine(_initNotification);
			_initNotification = null;
		}

		ControlNotification.text = notification;
		ControlNotification.CrossFadeAlpha(1f, 0f, true);
		if (_fadeNotification != null) StopCoroutine(_fadeNotification);
		_fadeNotification = StartCoroutine(FadeNotification());
	}

	private IEnumerator FadeNotification()
	{
		yield return new WaitForSecondsRealtime(NotificationFadeDelay);
		ControlNotification.CrossFadeAlpha(0f, NotificationFadeTime, true);
	}
}
}