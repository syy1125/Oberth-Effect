using System;
using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Common;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.Vehicle
{
public class VehicleResourceManager : MonoBehaviour
{
	private List<ResourceStorageBlock> _storageBlocks;
	private bool _storageChanged;
	private Dictionary<VehicleResource, float> _resourceCapacities;

	private List<ResourceGeneratorBlock> _generatorBlocks;

	private Dictionary<VehicleResource, float> _currentResources;

	private void Awake()
	{
		_storageBlocks = new List<ResourceStorageBlock>();
		_storageChanged = false;
		_resourceCapacities = new Dictionary<VehicleResource, float>();

		_generatorBlocks = new List<ResourceGeneratorBlock>();

		_currentResources = new Dictionary<VehicleResource, float>();
	}

	#region Resource Block Access

	public void AddStorage(ResourceStorageBlock block)
	{
		_storageBlocks.Add(block);
		_storageChanged = true;
	}

	public void RemoveStorage(ResourceStorageBlock block)
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

	public void AddGenerator(ResourceGeneratorBlock block)
	{
		_generatorBlocks.Add(block);
	}

	public void RemoveGenerator(ResourceGeneratorBlock block)
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
		if (_storageChanged)
		{
			_resourceCapacities.Clear();
			SumResources(_storageBlocks.Select(block => block.ResourceCapacityDict), _resourceCapacities);

			_storageChanged = false;
		}

		SumResources(
			_generatorBlocks
				.Select(generator => generator.GenerateResources())
				.Where(dict => dict != null),
			_currentResources
		);

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

	private static void SumResources(
		IEnumerable<Dictionary<VehicleResource, float>> sources,
		IDictionary<VehicleResource, float> output
	)
	{
		foreach (Dictionary<VehicleResource, float> source in sources)
		{
			foreach (KeyValuePair<VehicleResource, float> entry in source)
			{
				output[entry.Key] = output.TryGetValue(entry.Key, out float value) ? value + entry.Value : entry.Value;
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
}