using System.Collections.Generic;
using Syy1125.OberthEffect.Blocks;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.Vehicle
{
public class VehicleResourceManager : MonoBehaviour
{
	private List<ResourceStorageBlock> _storageBlocks;
	private bool _storageChanged;
	private float _maxEnergy;
	private float _maxFuel;

	private List<ResourceGeneratorBlock> _generatorBlocks;

	private float _currentEnergy;
	private float _currentFuel;

	private void Awake()
	{
		_storageBlocks = new List<ResourceStorageBlock>();
		_storageChanged = false;
		_maxEnergy = 0;
		_maxFuel = 0;

		_generatorBlocks = new List<ResourceGeneratorBlock>();

		_currentEnergy = 0;
		_currentFuel = 0;
	}

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

	private void FixedUpdate()
	{
		if (_storageChanged)
		{
			_maxEnergy = 0f;
			_maxFuel = 0f;

			foreach (ResourceStorageBlock block in _storageBlocks)
			{
				_maxEnergy += block.EnergyCapacity;
				_maxFuel += block.FuelCapacity;
			}

			_storageChanged = false;
		}

		var energyGeneration = 0f;
		var fuelGeneration = 0f;

		foreach (ResourceGeneratorBlock block in _generatorBlocks)
		{
			energyGeneration += block.GenerateEnergy();
			fuelGeneration += block.GenerateFuel();
		}

		_currentEnergy = Mathf.Clamp(_currentEnergy + energyGeneration * Time.fixedDeltaTime, 0f, _maxEnergy);
		_currentFuel = Mathf.Clamp(_currentFuel + fuelGeneration * Time.fixedDeltaTime, 0f, _maxFuel);
	}
}
}