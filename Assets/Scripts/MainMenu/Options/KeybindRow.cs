using System;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Syy1125.OberthEffect.Init;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.MainMenu.Options
{
public class KeybindRow : MonoBehaviour
{
	[Header("Input")]
	public InputActionReference TargetAction;
	public int PrimaryBindingIndex = 0;
	public int SecondaryBindingIndex = -1;
	public InputActionReference[] IgnoreConflicts;

	[Header("Config")]
	public string CancelControlPath = "<Keyboard>/escape";

	[Header("References")]
	public Text Label;
	public Button PrimaryBinding;
	public Button SecondaryBinding;
	public Button ResetButton;

	private InputAction[] _conflictingActions;
	private int _rebindIndex;
	private InputActionRebindingExtensions.RebindingOperation _operation;

	private void OnEnable()
	{
		UpdateBindingDisplay();
		PrimaryBinding.onClick.AddListener(RebindPrimary);
		SecondaryBinding.onClick.AddListener(RebindSecondary);
		ResetButton.onClick.AddListener(ResetBindings);
	}

	private void Start()
	{
		_conflictingActions = TargetAction.action.actionMap.actions
			.Except(IgnoreConflicts.Select(item => item.action))
			.ToArray();
	}

	private void OnDisable()
	{
		PrimaryBinding.onClick.RemoveListener(RebindPrimary);
		SecondaryBinding.onClick.RemoveListener(RebindSecondary);
		ResetButton.onClick.RemoveListener(ResetBindings);
	}

	public void UpdateBindingDisplay()
	{
		var action = TargetAction.action;

		InputBinding primary = action.bindings[PrimaryBindingIndex];
		PrimaryBinding.GetComponentInChildren<Text>().text = InputControlPath.ToHumanReadableString(
			primary.effectivePath,
			InputControlPath.HumanReadableStringOptions.OmitDevice
		);
		bool hasOverride = KeybindManager.HasOverride(primary);

		if (SecondaryBindingIndex >= 0)
		{
			InputBinding secondary = action.bindings[SecondaryBindingIndex];
			SecondaryBinding.GetComponentInChildren<Text>().text = InputControlPath.ToHumanReadableString(
				secondary.effectivePath,
				InputControlPath.HumanReadableStringOptions.OmitDevice
			);
			hasOverride |= KeybindManager.HasOverride(secondary);
		}
		else
		{
			SecondaryBinding.GetComponentInChildren<Text>().text = "";
			SecondaryBinding.interactable = false;
		}

		ResetButton.interactable = hasOverride;
	}

	private void RebindPrimary()
	{
		BeginRebind(PrimaryBindingIndex);
	}

	private void RebindSecondary()
	{
		BeginRebind(SecondaryBindingIndex);
	}

	private void BeginRebind(int index)
	{
		_rebindIndex = index;

		_operation = TargetAction.action.PerformInteractiveRebinding(index)
			// Pointer position and delta automatically excluded by PerformInteractiveRebinding
			.WithControlsExcluding("<Keyboard>/anyKey");

		// PerformInteractiveRebinding sets the delay to 0.05f by default.
		// However, having a delay breaks OnPotentialMatch logic, which we rely on for notifying keybind conflicts.
		// Reset the delay to 0 to get around that issue.
		_operation.OnMatchWaitForAnother(0f);

		if (!string.IsNullOrWhiteSpace(CancelControlPath))
		{
			_operation.WithCancelingThrough(CancelControlPath);
		}

		_operation
			.OnPotentialMatch(CheckConflicts)
			.OnApplyBinding(ApplyBinding)
			.OnComplete(CompleteBinding)
			.OnCancel(ClearBinding);

		_operation.Start();

		GetComponentInParent<KeybindScreen>()
			.SetBindingStatus($"Press new key to bind to <color=\"orange\">{Label.text}</color>");
	}

	private void CheckConflicts(InputActionRebindingExtensions.RebindingOperation operation)
	{
		bool warned = false;

		while (operation.selectedControl != null)
		{
			var conflict = FindConflict(operation.selectedControl);

			if (conflict == null)
			{
				operation.Complete();
				return;
			}

			if (!warned)
			{
				string pathName = InputControlPath.ToHumanReadableString(operation.selectedControl.path);
				string actionName = GetActionName(conflict.Item1, conflict.Item2);

				GetComponentInParent<KeybindScreen>().SetBindingStatus(
					$"<color=\"orange\">{pathName}</color> is already assigned to <color=\"orange\">{actionName}</color>"
				);

				warned = true;
			}

			operation.RemoveCandidate(operation.selectedControl);
		}
	}

	private Tuple<InputAction, int> FindConflict(InputControl targetControl)
	{
		foreach (InputAction action in _conflictingActions)
		{
			for (int i = 0; i < action.bindings.Count; i++)
			{
				if (action.bindings[i].isComposite) continue;
				// Ignore self conflict
				if (action == TargetAction.action && i == _rebindIndex) continue;

				InputBinding binding = action.bindings[i];
				if (string.IsNullOrEmpty(binding.effectivePath)) continue;

				if (InputControlPath.Matches(binding.effectivePath, targetControl))
				{
					return Tuple.Create(action, i);
				}
			}
		}

		return null;
	}

	private string GetActionName(InputAction action, int index)
	{
		foreach (KeybindRow row in GetComponentInParent<KeybindScreen>().GetComponentsInChildren<KeybindRow>())
		{
			if (
				row.TargetAction.action.Equals(action)
				&& (row.PrimaryBindingIndex == index || row.SecondaryBindingIndex == index)
			)
			{
				return row.Label.text;
			}
		}

		return $"{action.actionMap.name}/{action.name}";
	}

	private void ApplyBinding(InputActionRebindingExtensions.RebindingOperation operation, string path)
	{
		if (path != TargetAction.action.bindings[_rebindIndex].effectivePath)
		{
			TargetAction.action.ApplyBindingOverride(_rebindIndex, path);
			Debug.Log($"{Label.text} rebound to {TargetAction.action.bindings[_rebindIndex].effectivePath}");
			GetComponentInParent<KeybindScreen>().AfterRebind();
		}
	}

	private void ClearBinding(InputActionRebindingExtensions.RebindingOperation operation)
	{
		Debug.Log($"Clearing binding for {Label.text}");

		TargetAction.action.ApplyBindingOverride(_rebindIndex, "");

		GetComponentInParent<KeybindScreen>().AfterRebind();
		EndRebind();
	}

	private void CompleteBinding(InputActionRebindingExtensions.RebindingOperation operation)
	{
		EndRebind();
	}

	private void EndRebind()
	{
		_operation.Dispose();
		_operation = null;

		UpdateBindingDisplay();
		GetComponentInParent<KeybindScreen>().CloseBindingOverlay();
	}

	private void ResetBindings()
	{
		TargetAction.action.RemoveAllBindingOverrides();
		UpdateBindingDisplay();
	}
}
}