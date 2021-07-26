using System;
using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Resource
{
public class FreeResourceGeneratorBlock : MonoBehaviour, IResourceGeneratorBlock, ITooltipProvider
{
	[NonSerialized]
	public Dictionary<string, float> GenerationRate;

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

	public IReadOnlyDictionary<string, float> GetGenerationRate()
	{
		return GenerationRate;
	}

	public string GetTooltip()
	{
		return "Passive resource generation\n"
		       + string.Join(
			       "\n",
			       VehicleResourceDatabase.Instance.FormatResourceDict(GenerationRate)
				       .Select(line => $"  {line}")
		       );
	}

	public IReadOnlyDictionary<string, float> GetMaxGenerationRate()
	{
		return GenerationRate;
	}
}
}