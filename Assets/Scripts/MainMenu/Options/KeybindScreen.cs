using System;
using System.Linq;
using Syy1125.OberthEffect.Components.UserInterface;
using Syy1125.OberthEffect.Init;
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
		ResetButton.interactable = InputActions.Any(KeybindManager.HasOverride);
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
		ResetButton.interactable = InputActions.Any(KeybindManager.HasOverride);
	}

	private void BackToMenu()
	{
		KeybindManager.Instance.LoadKeybinds();
		OnClose.Invoke();
	}

	private void RevertBindings()
	{
		KeybindManager.Instance.LoadKeybinds();

		foreach (KeybindRow row in GetComponentsInChildren<KeybindRow>())
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

		foreach (KeybindRow row in GetComponentsInChildren<KeybindRow>())
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