using System.Collections.Generic;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Spec.ControlGroup;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.Construct
{
public class VehicleControlGroupProxy : MonoBehaviour, IControlConditionProvider
{
	private List<IControlConditionReceiver> _receivers;

	private void Awake()
	{
		_receivers = new List<IControlConditionReceiver>();
	}

	private void OnEnable()
	{
		if (PlayerControlConfig.Instance != null)
		{
			PlayerControlConfig.Instance.AnyControlGroupChanged.AddListener(BroadcastControlChange);
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

	private void BroadcastControlChange(List<string> _)
	{
		foreach (IControlConditionReceiver receiver in _receivers)
		{
			receiver.OnControlGroupsChanged(this);
		}
	}

	public bool IsConditionTrue(ControlConditionSpec condition)
	{
		return condition == null
		       || PlayerControlConfig.Instance == null
		       || PlayerControlConfig.Instance.IsConditionTrue(condition);
	}
}
}