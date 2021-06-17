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
public class VehicleResourceManager :
	MonoBehaviourPun,
	IPunObservable,
	IResourceStorageBlockRegistry,
	IResourceGeneratorBlockRegistry,
	IResourceConsumerBlockRegistry
{
	private bool _isMine;

	private List<ResourceStorageBlock> _storageBlocks;
	private bool _storageChanged;
	private Dictionary<VehicleResource, float> _resourceCapacities;

	private List<IResourceGeneratorBlock> _generatorBlocks;
	private Dictionary<VehicleResource, float> _currentResources;

	private List<IResourceConsumerBlock> _consumerBlocks;
	private Dictionary<VehicleResource, float> _resourceRequestRate;
	private Dictionary<VehicleResource, float> _resourceSatisfaction;

	private void Awake()
	{
		_storageBlocks = new List<ResourceStorageBlock>();
		_storageChanged = false;
		_resourceCapacities = new Dictionary<VehicleResource, float>();

		_generatorBlocks = new List<IResourceGeneratorBlock>();
		_currentResources = new Dictionary<VehicleResource, float>();

		_consumerBlocks = new List<IResourceConsumerBlock>();
		_resourceRequestRate = new Dictionary<VehicleResource, float>();
		_resourceSatisfaction = new Dictionary<VehicleResource, float>();
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

	public void RegisterBlock(IResourceGeneratorBlock block)
	{
		_generatorBlocks.Add(block);
	}

	public void UnregisterBlock(IResourceGeneratorBlock block)
	{
		bool success = _generatorBlocks.Remove(block);
		if (!success)
		{
			Debug.LogError($"Failed to remove resource generator block {block}");
		}
	}

	public void RegisterBlock(IResourceConsumerBlock block)
	{
		_consumerBlocks.Add(block);
	}

	public void UnregisterBlock(IResourceConsumerBlock block)
	{
		bool success = _consumerBlocks.Remove(block);
		if (!success)
		{
			Debug.LogError($"Failed to remove resource consumer block {block}");
		}
	}

	#endregion

	#region Update

	private void FixedUpdate()
	{
		if (_isMine)
		{
			if (_storageChanged)
			{
				UpdateStorage();
			}

			GenerateResources();
			ClampCurrentResources();

			UpdateResourceSatisfaction();
		}

		SatisfyConsumers(_isMine);
	}

	private void UpdateStorage()
	{
		_resourceCapacities.Clear();
		DictionaryUtils.AddDictionaries(
			_storageBlocks.Select(block => block.ResourceCapacityDict),
			_resourceCapacities
		);

		_storageChanged = false;
	}

	private void GenerateResources()
	{
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

	private void UpdateResourceSatisfaction()
	{
		_resourceRequestRate.Clear();
		DictionaryUtils.AddDictionaries(
			_consumerBlocks.Select(block => block.GetResourceConsumptionRateRequest()),
			_resourceRequestRate
		);

		_resourceSatisfaction.Clear();
		foreach (KeyValuePair<VehicleResource, float> pair in _resourceRequestRate)
		{
			_resourceSatisfaction.Add(
				pair.Key,
				_currentResources.TryGetValue(pair.Key, out float value)
					? Mathf.Clamp01(value / (pair.Value * Time.fixedDeltaTime))
					: 0f
			);
		}
	}

	private void SatisfyConsumers(bool consumeResources)
	{
		foreach (IResourceConsumerBlock consumer in _consumerBlocks)
		{
			IDictionary<VehicleResource, float> request = consumer.GetResourceConsumptionRateRequest();
			float satisfactionLevel = Mathf.Min(
				request.Keys
					.Select(resource => _resourceSatisfaction.TryGetValue(resource, out float level) ? level : 0f)
					.ToArray()
			);

			if (consumeResources && satisfactionLevel > 0f)
			{
				foreach (KeyValuePair<VehicleResource, float> pair in request)
				{
					_currentResources[pair.Key] -= pair.Value * satisfactionLevel * Time.fixedDeltaTime;
				}
			}

			consumer.SatisfyResourceRequestAtLevel(satisfactionLevel);
		}
	}

	#endregion

	#region PUN

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(_resourceSatisfaction.Count);
			foreach (KeyValuePair<VehicleResource, float> pair in _resourceSatisfaction)
			{
				stream.SendNext(pair.Key.Id);
				stream.SendNext(pair.Value);
			}
		}
		else
		{
			_resourceSatisfaction.Clear();
			int count = (int) stream.ReceiveNext();
			for (int i = 0; i < count; i++)
			{
				string resourceId = (string) stream.ReceiveNext();
				float satisfaction = (float) stream.ReceiveNext();
				_resourceSatisfaction.Add(VehicleResourceDatabase.Instance.GetResource(resourceId), satisfaction);
			}
		}
	}

	#endregion

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