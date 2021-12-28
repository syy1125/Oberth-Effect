using System;
using Syy1125.OberthEffect.Init;
using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Spec;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.MainMenu.Options
{
// This class overlaps a lot with KeybindRow but there are enough differences that code sharing is currently difficult.
// Is there a better way of managing control groups that lets me share code?
public class ControlGroupRow : MonoBehaviour, IKeybindRow
{
	[NonSerialized]
	public string ControlGroupId;
	[NonSerialized]
	public InputActionMap SimulationActionMap;

	[Header("References")]
	public Text Label;
	public Button BindingButton;
	public Button ResetButton;

	private InputActionRebindingExtensions.RebindingOperation _operation;

	private void OnEnable()
	{
		UpdateBindingDisplay();
	}

	private void Start()
	{
		BindingButton.onClick.AddListener(Rebind);
		ResetButton.onClick.AddListener(ResetBinding);
	}

	public void UpdateBindingDisplay()
	{
		if (string.IsNullOrEmpty(ControlGroupId)) return;

		string effectivePath = KeybindManager.Instance.GetControlGroupPath(ControlGroupId);
		BindingButton.GetComponentInChildren<Text>().text = InputControlPath.ToHumanReadableString(
			effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice
		);
		bool hasOverride = KeybindManager.Instance.ControlGroupHasOverride(ControlGroupId);

		ResetButton.interactable = hasOverride;
	}

	private void Rebind()
	{
		_operation = new InputActionRebindingExtensions.RebindingOperation()
			.WithExpectedControlType("Button")
			.WithControlsExcluding("<Pointer>/delta")
			.WithControlsExcluding("<Pointer>/position")
			.WithControlsExcluding("<Keyboard>/anyKey")
			.WithControlsExcluding("<Mouse>/clickCount")
			.WithMatchingEventsBeingSuppressed();

		_operation.WithCancelingThrough("<Keyboard>/escape");

		_operation
			.OnPotentialMatch(CheckConflicts)
			.OnApplyBinding(ApplyBinding)
			.OnCancel(ClearBinding)
			.OnComplete(CompleteBinding);

		_operation.Start();

		GetComponentInParent<KeybindScreen>()
			.SetBindingStatus($"Press new key to bind to <color=\"orange\">{Label.text}</color>");
	}

	private void CheckConflicts(InputActionRebindingExtensions.RebindingOperation operation)
	{
		bool warned = false;

		while (operation.selectedControl != null)
		{
			var conflictName = FindConflict(operation.selectedControl);

			if (conflictName == null)
			{
				operation.Complete();
				return;
			}

			if (!warned)
			{
				string pathName = InputControlPath.ToHumanReadableString(operation.selectedControl.path);

				GetComponentInParent<KeybindScreen>().SetBindingStatus(
					$"<color=\"orange\">{pathName}</color> is already assigned to <color=\"orange\">{conflictName}</color>"
				);

				warned = true;
			}

			operation.RemoveCandidate(operation.selectedControl);
		}
	}

	private string FindConflict(InputControl targetControl)
	{
		foreach (InputAction action in SimulationActionMap.actions)
		{
			for (int i = 0; i < action.bindings.Count; i++)
			{
				if (action.bindings[i].isComposite) continue;

				InputBinding binding = action.bindings[i];
				if (string.IsNullOrEmpty(binding.effectivePath)) continue;

				if (InputControlPath.Matches(binding.effectivePath, targetControl))
				{
					return GetActionName(action, i);
				}
			}
		}

		foreach (SpecInstance<ControlGroupSpec> instance in ControlGroupDatabase.Instance.ListControlGroups())
		{
			if (instance.Spec.ControlGroupId == ControlGroupId) continue;

			string path = KeybindManager.Instance.GetControlGroupPath(instance.Spec.ControlGroupId);

			if (!string.IsNullOrEmpty(path) && InputControlPath.Matches(path, targetControl))
			{
				return StringUtils.ToTitleCase(instance.Spec.KeybindDescription);
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
		if (path != KeybindManager.Instance.GetControlGroupPath(ControlGroupId))
		{
			KeybindManager.Instance.SetControlGroupOverride(ControlGroupId, path);
			Debug.Log($"{Label.text} rebound to {path}");
			GetComponentInParent<KeybindScreen>().AfterRebind();
		}
	}

	private void ClearBinding(InputActionRebindingExtensions.RebindingOperation operation)
	{
		Debug.Log($"Clearing binding for {Label.text}");

		KeybindManager.Instance.SetControlGroupOverride(ControlGroupId, "");

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

	private void ResetBinding()
	{
		KeybindManager.Instance.RemoveControlGroupOverride(ControlGroupId);
		UpdateBindingDisplay();
		GetComponentInParent<KeybindScreen>().AfterRebind();
	}
}
}