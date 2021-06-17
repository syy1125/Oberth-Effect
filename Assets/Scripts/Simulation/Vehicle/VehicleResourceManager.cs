using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Utils;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.Vehicle
{
// Note that this class need to execute after all resource usage scripts in order to function properly.
public class VehicleResourceManager : MonoBehaviourPun, IResourceStorageBlockRegistry, IResourceGeneratorBlockRegistry
{
	private bool _isMine;

	private List<ResourceStorageBlock> _storageBlocks;
	private bool _storageChanged;
	private Dictionary<VehicleResource, float> _resourceCapacities;

	private List<ResourceGeneratorBlock> _generatorBlocks;

	private Dictionary<VehicleResource, float> _currentResources;

	private List<IResourceConsumer> _consumers;
	private SortedDictionary<int, List<IResourceConsumer>> _orderedConsumers;
	private Dictionary<VehicleResource, float> _resourceRequestRate;

	private void Awake()
	{
		_storageBlocks = new List<ResourceStorageBlock>();
		_storageChanged = false;
		_resourceCapacities = new Dictionary<VehicleResource, float>();

		_generatorBlocks = new List<ResourceGeneratorBlock>();

		_currentResources = new Dictionary<VehicleResource, float>();

		_consumers = GetComponents<MonoBehaviour>()
			.Select(behaviour => behaviour as IResourceConsumer)
			.Where(consumer => consumer != null)
			.ToList();
		_orderedConsumers = new SortedDictionary<int, List<IResourceConsumer>>(new ReverseIntComparator());
		_resourceRequestRate = new Dictionary<VehicleResource, float>();
	}

	private void Start()
	{
		_isMine = photonView == null || photonView.IsMine;
	}

	#region Resource Block Access

	public void RegisterBlock(ResourceStorageBlock block)
	{
		_storageBlocks.Add(block);
		_storageChanged = true;
	}

	public void UnregisterBlock(ResourceStorageBlock block)
	{
		bool success = _storageBlocks.Remove(block);
		if (success)
		{
			_storageChanged = true;
		}
		else
		{
			Debug.LogError($"Failed to remove resource storage block {block}");
		}
	}

	public void RegisterBlock(ResourceGeneratorBlock block)
	{
		_generatorBlocks.Add(block);
	}

	public void UnregisterBlock(ResourceGeneratorBlock block)
	{
		bool success = _generatorBlocks.Remove(block);
		if (!success)
		{
			Debug.LogError($"Failed to remove resource generator block {block}");
		}
	}

	#endregion

	private void FixedUpdate()
	{
		if (!_isMine) return;

		if (_storageChanged)
		{
			_resourceCapacities.Clear();
			DictionaryUtils.AddDictionaries(
				_storageBlocks.Select(block => block.ResourceCapacityDict),
				_resourceCapacities
			);

			_storageChanged = false;
		}

		DictionaryUtils.AddDictionaries(
			_generatorBlocks
				.Select(
					generator => generator.GetGenerationRate()?.ToDictionary(
						pair => pair.Key,
						pair => pair.Value * Time.fixedDeltaTime
					)
				)
				.Where(dict => dict != null && dict.Count > 0),
			_currentResources
		);

		ClampCurrentResources();

		_orderedConsumers.Clear();
		foreach (IResourceConsumer consumer in _consumers)
		{
			int priority = consumer.GetResourcePriority();

			if (!_orderedConsumers.ContainsKey(priority))
			{
				_orderedConsumers.Add(priority, new List<IResourceConsumer>());
			}

			_orderedConsumers[priority].Add(consumer);
		}

		_resourceRequestRate.Clear();
		var resourceUsage = new Dictionary<VehicleResource, float>();
		foreach (KeyValuePair<int, List<IResourceConsumer>> consumerPair in _orderedConsumers)
		{
			resourceUsage.Clear();

			DictionaryUtils.AddDictionaries(
				consumerPair.Value.Select(consumer => consumer.GetConsumptionRateRequest()),
				_resourceRequestRate
			);
			DictionaryUtils.AddDictionaries(
				consumerPair.Value.Select(
					consumer => consumer.GetConsumptionRateRequest().ToDictionary(
						pair => pair.Key, pair => pair.Value * Time.deltaTime
					)
				),
				resourceUsage
			);

			float satisfaction = 1;

			foreach (KeyValuePair<VehicleResource, float> request in resourceUsage)
			{
				if (_currentResources.TryGetValue(request.Key, out float available))
				{
					satisfaction = Mathf.Min(satisfaction, available / request.Value);
				}
				else
				{
					satisfaction = 0;
					break;
				}
			}

			satisfaction = Mathf.Clamp01(satisfaction);

			foreach (IResourceConsumer consumer in consumerPair.Value)
			{
				consumer.SatisfyResourceRequestAtLevel(satisfaction);
			}

			if (satisfaction > 0)
			{
				foreach (KeyValuePair<VehicleResource, float> usagePair in resourceUsage)
				{
					_currentResources[usagePair.Key] -= usagePair.Value * satisfaction;
				}
			}

			ClampCurrentResources();
		}
	}

	private void ClampCurrentResources()
	{
		foreach (VehicleResource resource in _currentResources.Keys.ToArray())
		{
			if (_resourceCapacities.TryGetValue(resource, out float capacity))
			{
				_currentResources[resource] = Mathf.Clamp(_currentResources[resource], 0, capacity);
			}
			else
			{
				_currentResources.Remove(resource);
			}
		}
	}

	#region Public Access

	// Returns tuple (current, capacity), or null if the vehicle is not capable of holding the specified resource
	public Tuple<float, float> GetResourceStatus(VehicleResource resource)
	{
		if (_resourceCapacities.TryGetValue(resource, out float capacity))
		{
			return new Tuple<float, float>(
				_currentResources.TryGetValue(resource, out float stored) ? stored : 0f, capacity
			);
		}
		else
		{
			return null;
		}
	}

	#endregion
}

internal class ReverseIntComparator : IComparer<int>
{
	public int Compare(int x, int y)
	{
		return y - x;
	}
}
}