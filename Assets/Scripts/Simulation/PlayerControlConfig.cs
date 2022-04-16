using System;
using System.Collections.Generic;
using Syy1125.OberthEffect.Common.Init;
using Syy1125.OberthEffect.Components.UserInterface;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.ControlCondition;
using Syy1125.OberthEffect.Simulation.UserInterface;
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

	public InputActionReference CycleHudModeAction;
	public InputActionReference ToggleInertiaDampenerAction;
	public InputActionReference CycleControlModeAction;
	public InputActionReference InvertAimAction;

	public HeadsUpDisplayMode HudMode { get; private set; }
	public UnityEvent HudModeChanged;

	public bool InertiaDampenerActive { get; private set; }
	public UnityEvent InertiaDampenerChanged;

	public VehicleControlMode ControlMode { get; private set; }
	public UnityEvent ControlModeChanged;

	public bool InvertAim { get; private set; }
	public UnityEvent InvertAimChanged;

	public UnityEvent<List<string>> ControlGroupStateChanged;
	public UnityEvent ActiveControlGroupChanged;
	private HashSet<string> _activeControlGroups;

	private List<Tuple<string, InputAction>> _controlGroupActions;
	private Dictionary<string, int> _controlGroupStates;
	private List<string> _changed;

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
		CycleHudModeAction.action.performed += CycleHudMode;
		ToggleInertiaDampenerAction.action.performed += ToggleInertiaDampener;
		CycleControlModeAction.action.performed += CycleControlMode;
		InvertAimAction.action.performed += ToggleInvertAim;

		_controlGroupActions = new List<Tuple<string, InputAction>>();
		_controlGroupStates = new Dictionary<string, int>();
		_changed = new List<string>();

		foreach (SpecInstance<ControlGroupSpec> instance in ControlGroupDatabase.Instance.ListControlGroups())
		{
			InputAction action = new InputAction(
				instance.Spec.ControlGroupId, InputActionType.Button,
				KeybindManager.Instance.GetControlGroupPath(instance.Spec.ControlGroupId)
			);
			action.Enable();

			_controlGroupActions.Add(new Tuple<string, InputAction>(instance.Spec.ControlGroupId, action));
			_controlGroupStates.Add(instance.Spec.ControlGroupId, 0);
		}
	}

	private void Start()
	{
		HudMode = HeadsUpDisplayMode.Standard;
		HudModeChanged?.Invoke();

		InertiaDampenerActive = false;
		InertiaDampenerChanged?.Invoke();

		ControlMode = VehicleSelection.SelectedVehicle.DefaultControlMode;
		ControlModeChanged?.Invoke();

		InvertAim = false;
		InvertAimChanged?.Invoke();
	}

	private void OnDisable()
	{
		CycleHudModeAction.action.performed -= CycleHudMode;
		ToggleInertiaDampenerAction.action.performed -= ToggleInertiaDampener;
		CycleControlModeAction.action.performed -= CycleControlMode;
		InvertAimAction.action.performed -= ToggleInvertAim;

		foreach (Tuple<string, InputAction> tuple in _controlGroupActions)
		{
			tuple.Item2.Disable();
			tuple.Item2.Dispose();
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
		_changed.Clear();

		foreach (Tuple<string, InputAction> action in _controlGroupActions)
		{
			if (!IsControlGroupActive(action.Item1)) continue;

			if (action.Item2.triggered)
			{
				int stateCount = ControlGroupDatabase.Instance.GetSpecInstance(action.Item1).Spec.States.Length;
				_controlGroupStates[action.Item1] = (_controlGroupStates[action.Item1] + 1) % stateCount;
				_changed.Add(action.Item1);
			}
		}

		if (_changed.Count > 0)
		{
			ControlGroupStateChanged.Invoke(_changed);
		}
	}

	private void CycleHudMode(InputAction.CallbackContext context)
	{
		HudMode = HudMode switch
		{
			HeadsUpDisplayMode.Standard => HeadsUpDisplayMode.Extended,
			HeadsUpDisplayMode.Extended => HeadsUpDisplayMode.Minimal,
			HeadsUpDisplayMode.Minimal => HeadsUpDisplayMode.Standard,
			_ => throw new ArgumentOutOfRangeException()
		};

		HudModeChanged?.Invoke();
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
			VehicleControlMode.Mouse => VehicleControlMode.Relative,
			VehicleControlMode.Relative => VehicleControlMode.Cruise,
			VehicleControlMode.Cruise => VehicleControlMode.Mouse,
			_ => throw new ArgumentOutOfRangeException()
		};

		ControlModeChanged?.Invoke();
	}

	private void ToggleInvertAim(InputAction.CallbackContext context)
	{
		InvertAim = !InvertAim;
		InvertAimChanged?.Invoke();
	}

	#endregion

	public void SetActiveControlGroups(IEnumerable<string> controlGroups)
	{
		_activeControlGroups = new HashSet<string>(controlGroups);
		ActiveControlGroupChanged?.Invoke();
	}

	public bool IsControlGroupActive(string controlGroupId)
	{
		return _activeControlGroups == null || _activeControlGroups.Contains(controlGroupId);
	}

	public int GetStateIndex(string controlGroupId)
	{
		return _controlGroupStates[controlGroupId];
	}

	public string GetStateId(string controlGroupId)
	{
		return ControlGroupDatabase.Instance.GetSpecInstance(controlGroupId).Spec
			.States[_controlGroupStates[controlGroupId]].StateId;
	}

	public bool IsConditionTrue(IControlCondition condition)
	{
		return condition == null || condition.IsTrue(GetStateId);
	}
}
}