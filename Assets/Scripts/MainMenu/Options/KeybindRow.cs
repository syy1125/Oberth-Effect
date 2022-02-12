using System.Linq;
using Syy1125.OberthEffect.Common.Init;
using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Spec;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.MainMenu.Options
{
public class KeybindRow : MonoBehaviour, IKeybindRow
{
	[Header("Input")]
	public InputActionReference TargetAction;
	public int PrimaryBindingIndex = 0;
	public int SecondaryBindingIndex = -1;
	public bool ConflictWithControlGroups;
	public InputActionReference[] IgnoreConflicts;

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
	}

	private void Start()
	{
		_conflictingActions = TargetAction.action.actionMap.actions
			.Except(IgnoreConflicts.Select(item => item.action))
			.ToArray();

		PrimaryBinding.onClick.AddListener(RebindPrimary);
		SecondaryBinding.onClick.AddListener(RebindSecondary);
		ResetButton.onClick.AddListener(ResetBindings);
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

		_operation.WithCancelingThrough("<Keyboard>/escape");

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
					return GetActionName(action, i);
				}
			}
		}

		if (ConflictWithControlGroups)
		{
			foreach (SpecInstance<ControlGroupSpec> instance in ControlGroupDatabase.Instance.ListControlGroups())
			{
				string path = KeybindManager.Instance.GetControlGroupPath(instance.Spec.ControlGroupId);

				if (!string.IsNullOrEmpty(path) && InputControlPath.Matches(path, targetControl))
				{
					return StringUtils.ToTitleCase(instance.Spec.KeybindDescription);
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
		TargetAction.action.RemoveBindingOverride(PrimaryBindingIndex);
		if (SecondaryBindingIndex >= 0) TargetAction.action.RemoveBindingOverride(SecondaryBindingIndex);

		UpdateBindingDisplay();
		GetComponentInParent<KeybindScreen>().AfterRebind();
	}
}
}