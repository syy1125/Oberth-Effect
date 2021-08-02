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
	private Dictionary<string, float> _generationRate;

	private void OnEnable()
	{
		ExecuteEvents.ExecuteHierarchy<IResourceGeneratorBlockRegistry>(
			gameObject, null, (handler, _) => handler.RegisterBlock(this)
		);
	}

	public void LoadSpec(Dictionary<string, float> spec)
	{
		_generationRate = spec;
	}

	private void OnDisable()
	{
		ExecuteEvents.ExecuteHierarchy<IResourceGeneratorBlockRegistry>(
			gameObject, null, (handler, _) => handler.UnregisterBlock(this)
		);
	}

	public IReadOnlyDictionary<string, float> GetGenerationRate()
	{
		return _generationRate;
	}

	public string GetTooltip()
	{
		return "Passive resource generation\n"
		       + string.Join(
			       "\n",
			       VehicleResourceDatabase.Instance.FormatResourceDict(_generationRate)
				       .Select(line => $"  {line}")
		       );
	}

	public IReadOnlyDictionary<string, float> GetMaxGenerationRate()
	{
		return _generationRate;
	}
}
}