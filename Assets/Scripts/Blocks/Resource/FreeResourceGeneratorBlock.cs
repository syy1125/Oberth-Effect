using System;
using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Common;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Resource
{
public class FreeResourceGeneratorBlock : MonoBehaviour, IResourceGeneratorBlock
{
	public ResourceEntry[] GenerationRate;
	private Dictionary<VehicleResource, float> _generationRate;

	private void Awake()
	{
		_generationRate = GenerationRate.ToDictionary(
			entry => entry.Resource,
			entry => entry.Amount
		);
	}

	private void OnEnable()
	{
		ExecuteEvents.ExecuteHierarchy<IResourceGeneratorBlockRegistry>(
			gameObject, null, (handler, _) => handler.RegisterBlock(this)
		);
	}

	private void OnDisable()
	{
		ExecuteEvents.ExecuteHierarchy<IResourceGeneratorBlockRegistry>(
			gameObject, null, (handler, _) => handler.UnregisterBlock(this)
		);
	}

	public Dictionary<VehicleResource, float> GetGenerationRate()
	{
		return _generationRate;
	}
}
}