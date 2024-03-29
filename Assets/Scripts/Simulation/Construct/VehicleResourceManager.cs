using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.Lib.Utils;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.Construct
{
// Note that this class need to execute after all resource usage scripts in order to function properly.
[RequireComponent(typeof(VehicleCore))]
public class VehicleResourceManager :
	MonoBehaviourPun,
	IPunObservable,
	IResourceStorageRegistry,
	IResourceGeneratorRegistry,
	IResourceConsumerRegistry
{
	private VehicleCore _core;

	private List<IResourceStorage> _storageBlocks;
	private bool _storageChanged;
	private Dictionary<string, float> _resourceCapacities;

	private List<IResourceGenerator> _generatorBlocks;
	private Dictionary<string, float> _generationRate;
	private Dictionary<string, float> _currentResources;

	private List<IResourceConsumer> _consumerBlocks;
	private Dictionary<string, float> _resourceRequestRate;
	private Dictionary<string, float> _resourceSatisfaction;

	private void Awake()
	{
		_core = GetComponent<VehicleCore>();

		_storageBlocks = new List<IResourceStorage>();
		_storageChanged = false;
		_resourceCapacities = new Dictionary<string, float>();

		_generatorBlocks = new List<IResourceGenerator>();
		_generationRate = new Dictionary<string, float>();
		_currentResources = new Dictionary<string, float>();

		_consumerBlocks = new List<IResourceConsumer>();
		_resourceRequestRate = new Dictionary<string, float>();
		_resourceSatisfaction = new Dictionary<string, float>();
	}

	private void Start()
	{
		_core.AfterLoad(FillStorage);
	}

	#region Resource Block Access

	public void RegisterBlock(IResourceStorage block)
	{
		_storageBlocks.Add(block);
		_storageChanged = true;
	}

	public void UnregisterBlock(IResourceStorage block)
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

	public void RegisterBlock(IResourceGenerator block)
	{
		_generatorBlocks.Add(block);
	}

	public void UnregisterBlock(IResourceGenerator block)
	{
		bool success = _generatorBlocks.Remove(block);
		if (!success)
		{
			Debug.LogError($"Failed to remove resource generator block {block}");
		}
	}

	public void RegisterBlock(IResourceConsumer block)
	{
		_consumerBlocks.Add(block);
	}

	public void UnregisterBlock(IResourceConsumer block)
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
		if (photonView.IsMine)
		{
			if (_storageChanged)
			{
				UpdateStorage();
			}

			GenerateResources();
			ClampCurrentResources();

			UpdateResourceSatisfaction();
		}

		SatisfyConsumers(photonView.IsMine);
	}

	private void UpdateStorage()
	{
		_resourceCapacities.Clear();
		DictionaryUtils.SumDictionaries(
			_storageBlocks.Select(block => block.GetCapacity()),
			_resourceCapacities
		);

		_storageChanged = false;
	}

	private void GenerateResources()
	{
		_generationRate.Clear();
		DictionaryUtils.SumDictionaries(
			_generatorBlocks
				.Select(generator => generator.GetGenerationRate())
				.Where(dict => dict != null && dict.Count > 0),
			_generationRate
		);
		DictionaryUtils.AddDictionary(
			_generationRate.ToDictionary(entry => entry.Key, entry => entry.Value * Time.fixedDeltaTime),
			_currentResources
		);
	}

	private void ClampCurrentResources()
	{
		foreach (string resource in _currentResources.Keys.ToArray())
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
		DictionaryUtils.SumDictionaries(
			_consumerBlocks
				.Select(block => block.GetResourceConsumptionRateRequest())
				.Where(request => request != null),
			_resourceRequestRate
		);

		_resourceSatisfaction.Clear();
		foreach (KeyValuePair<string, float> pair in _resourceRequestRate)
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
		foreach (IResourceConsumer consumer in _consumerBlocks)
		{
			IReadOnlyDictionary<string, float> request = consumer.GetResourceConsumptionRateRequest();
			if (request == null) continue;

			float satisfactionLevel = Mathf.Min(
				request.Keys
					.Select(resource => _resourceSatisfaction.TryGetValue(resource, out float level) ? level : 0f)
					.Prepend(1f)
					.ToArray()
			);

			float consumptionLevel = consumer.SatisfyResourceRequestAtLevel(satisfactionLevel);

			if (consumeResources && consumptionLevel > 0f)
			{
				foreach (KeyValuePair<string, float> pair in request)
				{
					_currentResources[pair.Key] -= pair.Value * satisfactionLevel * Time.fixedDeltaTime;
				}
			}
		}
	}

	#endregion

	#region PUN

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(_resourceSatisfaction.Count);
			foreach (KeyValuePair<string, float> pair in _resourceSatisfaction)
			{
				stream.SendNext(pair.Key);
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
				_resourceSatisfaction.Add(resourceId, satisfaction);
			}
		}
	}

	#endregion

	private void FillStorage()
	{
		UpdateStorage();
		_currentResources = new Dictionary<string, float>(_resourceCapacities);
	}

	#region External Access

	public class ResourceStatus
	{
		public float CurrentAmount;
		public float StorageCapacity;
		public float GenerationRate;
		public float ConsumptionRequestRate;
		public float Satisfaction;
	}

	// Returns null if the vehicle is not capable of holding the specified resource
	public ResourceStatus GetResourceStatus(string resource)
	{
		if (!_resourceCapacities.TryGetValue(resource, out float capacity)) return null;

		return new ResourceStatus
		{
			CurrentAmount = _currentResources.TryGetValue(resource, out float stored) ? stored : 0f,
			StorageCapacity = capacity,
			GenerationRate = _generationRate.TryGetValue(resource, out float generation) ? generation : 0f,
			ConsumptionRequestRate =
				_resourceRequestRate.TryGetValue(resource, out float consumption) ? consumption : 0f,
			Satisfaction = _resourceSatisfaction.TryGetValue(resource, out float satisfaction) ? satisfaction : 1f
		};
	}

	public void AddResources(IReadOnlyDictionary<string, float> amount)
	{
		DictionaryUtils.AddDictionary(amount, _currentResources);
		ClampCurrentResources();
	}

	#endregion
}
}