using System.Linq;
using Syy1125.OberthEffect.Common.Init;
using Syy1125.OberthEffect.Components.UserInterface;
using Syy1125.OberthEffect.Spec;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.MainMenu.Options
{
public class KeybindScreen : MonoBehaviour
{
	[Header("Assets")]
	public InputActionAsset InputActions;

	[Header("References")]
	public GameObject BindingOverlay;
	public Text StatusText;

	public Button BackButton;
	public Button RevertButton;
	public Button ResetButton;
	public Button SaveButton;

	[Header("Events")]
	public UnityEvent OnClose;

	private bool _canRevert;

	private void OnEnable()
	{
		_canRevert = false;
		UpdateResetButton();
	}

	private void Start()
	{
		BackButton.onClick.AddListener(BackToMenu);
		RevertButton.onClick.AddListener(RevertBindings);
		ResetButton.onClick.AddListener(ResetBindings);
		SaveButton.onClick.AddListener(SaveBindings);
	}

	private void Update()
	{
		RevertButton.interactable = KeybindManager.Instance.Ready && _canRevert;

		if (KeybindManager.Instance.Ready)
		{
			RevertButton.interactable = _canRevert;
			RevertButton.GetComponent<Tooltip>().SetTooltip(null);
		}
		else
		{
			RevertButton.interactable = false;
			RevertButton.GetComponent<Tooltip>().SetTooltip("A keybind save/load operation is currently in progress.");
		}
	}

	public void SetBindingStatus(string status)
	{
		BindingOverlay.SetActive(true);
		StatusText.text = status;
	}

	public void CloseBindingOverlay()
	{
		BindingOverlay.SetActive(false);
	}

	public void AfterRebind()
	{
		_canRevert = true;
		UpdateResetButton();
	}

	private void UpdateResetButton()
	{
		ResetButton.interactable =
			InputActions.Any(KeybindManager.HasOverride)
			|| ControlGroupDatabase.Instance.ListControlGroups().Any(
				instance => KeybindManager.Instance.ControlGroupHasOverride(instance.Spec.ControlGroupId)
			);
	}

	private void BackToMenu()
	{
		KeybindManager.Instance.LoadKeybinds();
		OnClose.Invoke();
	}

	private void RevertBindings()
	{
		KeybindManager.Instance.LoadKeybinds();

		foreach (var row in GetComponentsInChildren<IKeybindRow>())
		{
			row.UpdateBindingDisplay();
		}

		_canRevert = false;
		ResetButton.interactable = InputActions.Any(KeybindManager.HasOverride);
	}

	private void ResetBindings()
	{
		bool changed = false;

		foreach (InputAction action in InputActions)
		{
			if (KeybindManager.HasOverride(action))
			{
				action.RemoveAllBindingOverrides();
				changed = true;
			}
		}

		foreach (SpecInstance<ControlGroupSpec> instance in ControlGroupDatabase.Instance.ListControlGroups())
		{
			if (KeybindManager.Instance.ControlGroupHasOverride(instance.Spec.ControlGroupId))
			{
				KeybindManager.Instance.RemoveControlGroupOverride(instance.Spec.ControlGroupId);
				changed = true;
			}
		}

		foreach (var row in GetComponentsInChildren<IKeybindRow>())
		{
			row.UpdateBindingDisplay();
		}

		_canRevert |= changed;
		ResetButton.interactable = false;
	}

	private void SaveBindings()
	{
		KeybindManager.Instance.SaveKeybinds();
		OnClose.Invoke();
	}
}
}