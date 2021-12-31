using System.Collections.Generic;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Common.ControlCondition;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.Construct
{
public class VehicleControlGroupProxy : MonoBehaviour, IControlConditionProvider
{
	private List<IControlConditionReceiver> _receivers;
	private HashSet<string> _activeControlGroups;

	private void Awake()
	{
		_receivers = new List<IControlConditionReceiver>();
		_activeControlGroups = new HashSet<string>();
	}

	private void OnEnable()
	{
		if (PlayerControlConfig.Instance != null)
		{
			PlayerControlConfig.Instance.ControlGroupStateChanged.AddListener(BroadcastControlChange);
		}
	}

	public void RegisterBlock(IControlConditionReceiver block)
	{
		_receivers.Add(block);
		block.OnControlGroupsChanged(this);
	}

	public void UnregisterBlock(IControlConditionReceiver block)
	{
		bool success = _receivers.Remove(block);
		if (!success)
		{
			Debug.LogError($"Failed to remove control condition receiver {block}");
		}
	}

	public void MarkControlGroupsActive(IEnumerable<string> controlGroupIds)
	{
		foreach (string controlGroupId in controlGroupIds)
		{
			_activeControlGroups.Add(controlGroupId);
		}

		PlayerControlConfig.Instance.SetActiveControlGroups(_activeControlGroups);
	}

	private void BroadcastControlChange(List<string> _)
	{
		foreach (IControlConditionReceiver receiver in _receivers)
		{
			receiver.OnControlGroupsChanged(this);
		}
	}

	public bool IsConditionTrue(IControlCondition condition)
	{
		return PlayerControlConfig.Instance == null
		       || PlayerControlConfig.Instance.IsConditionTrue(condition);
	}
}
}