using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun.Demo.Cockpit;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Spec;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Simulation
{
public class PlayerControlConfig : MonoBehaviour
{
	public static PlayerControlConfig Instance;

	public InputActionReference ToggleInertiaDampenerAction;
	public InputActionReference CycleControlModeAction;

	public bool InertiaDampenerActive { get; private set; }
	public UnityEvent InertiaDampenerChanged;

	public VehicleControlMode ControlMode { get; private set; }
	public UnityEvent ControlModeChanged;

	public UnityEvent AnyControlGroupChanged;

	private List<Tuple<string, InputAction>> _controlGroupActions;
	private Dictionary<string, int> _controlGroupStates;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else if (Instance != this)
		{
			Destroy(this);
		}
	}

	private void OnEnable()
	{
		ToggleInertiaDampenerAction.action.Enable();
		ToggleInertiaDampenerAction.action.performed += ToggleInertiaDampener;
		CycleControlModeAction.action.Enable();
		CycleControlModeAction.action.performed += CycleControlMode;

		_controlGroupActions = new List<Tuple<string, InputAction>>();
		_controlGroupStates = new Dictionary<string, int>();
		foreach (SpecInstance<ControlGroupSpec> instance in ControlGroupDatabase.Instance.ListControlGroups())
		{
			InputAction action = new InputAction(
				instance.Spec.ControlGroupId, InputActionType.Button, instance.Spec.DefaultKeybind
			);
			action.Enable();
			_controlGroupActions.Add(new Tuple<string, InputAction>(instance.Spec.ControlGroupId, action));
			_controlGroupStates.Add(instance.Spec.ControlGroupId, 0);
		}
	}

	private void Start()
	{
		ControlMode = VehicleSelection.SelectedVehicle.DefaultControlMode;
		ControlModeChanged?.Invoke();

		InertiaDampenerActive = false;
		InertiaDampenerChanged?.Invoke();
	}

	private void OnDisable()
	{
		ToggleInertiaDampenerAction.action.performed -= ToggleInertiaDampener;
		ToggleInertiaDampenerAction.action.Disable();
		CycleControlModeAction.action.performed -= CycleControlMode;
		CycleControlModeAction.action.Disable();

		foreach (Tuple<string, InputAction> tuple in _controlGroupActions)
		{
			tuple.Item2.Disable();
		}

		_controlGroupActions.Clear();
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	#region Input Handlers

	private void Update()
	{
		bool triggered = false;

		foreach (Tuple<string, InputAction> action in _controlGroupActions)
		{
			if (action.Item2.triggered)
			{
				int stateCount = ControlGroupDatabase.Instance.GetSpecInstance(action.Item1).Spec.States.Length;
				_controlGroupStates[action.Item1] = (_controlGroupStates[action.Item1] + 1) % stateCount;
				triggered = true;
			}
		}

		if (triggered)
		{
			AnyControlGroupChanged.Invoke();
		}
	}

	private void ToggleInertiaDampener(InputAction.CallbackContext context)
	{
		InertiaDampenerActive = !InertiaDampenerActive;
		InertiaDampenerChanged?.Invoke();
	}

	private void CycleControlMode(InputAction.CallbackContext context)
	{
		ControlMode = ControlMode switch
		{
			VehicleControlMode.Mouse => VehicleControlMode.Cruise,
			VehicleControlMode.Cruise => VehicleControlMode.Mouse,
			_ => throw new ArgumentOutOfRangeException()
		};

		ControlModeChanged?.Invoke();
	}

	#endregion

	public int GetStateIndex(string controlGroupId)
	{
		return _controlGroupStates[controlGroupId];
	}

	public string GetStateId(string controlGroupId)
	{
		return ControlGroupDatabase.Instance.GetSpecInstance(controlGroupId).Spec
			.States[_controlGroupStates[controlGroupId]].StateId;
	}

	public bool IsConditionTrue(ControlConditionSpec condition)
	{
		if (condition.And != null)
		{
			return condition.And.All(IsConditionTrue);
		}
		else if (condition.Or != null)
		{
			return condition.Or.Any(IsConditionTrue);
		}
		else if (condition.Not != null)
		{
			return !IsConditionTrue(condition.Not);
		}
		else
		{
			return condition.MatchValues.Contains(GetStateId(condition.ControlGroupId));
		}
	}
}
}