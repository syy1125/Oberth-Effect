using System;
using System.Collections.Generic;
using Syy1125.OberthEffect.Spec.Block.Resource;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Resource
{
public interface IFusionGeneratorRegistry : IBlockRegistry<FusionGenerator>, IEventSystemHandler
{}

public class FusionGenerator : MonoBehaviour, IResourceGeneratorBlock
{
	private Dictionary<string, float> _generationRate;
	private Dictionary<string, float> _empty = new Dictionary<string, float>();
	private bool _active;
	private List<SpriteRenderer> _activeRenderers;

	private void OnEnable()
	{
		ExecuteEvents.ExecuteHierarchy<IResourceGeneratorBlockRegistry>(
			gameObject, null, (registry, _) => registry.RegisterBlock(this)
		);
		ExecuteEvents.ExecuteHierarchy<IFusionGeneratorRegistry>(
			gameObject, null, (registry, _) => registry.RegisterBlock(this)
		);
	}

	public void LoadSpec(FusionGeneratorSpec spec)
	{
		_generationRate = spec.GenerationRate;
		_activeRenderers = RendererHelper.AttachRenderers(transform, spec.ActiveRenderers);
	}

	private void OnDisable()
	{
		ExecuteEvents.ExecuteHierarchy<IResourceGeneratorBlockRegistry>(
			gameObject, null, (registry, _) => registry.UnregisterBlock(this)
		);
		ExecuteEvents.ExecuteHierarchy<IFusionGeneratorRegistry>(
			gameObject, null, (registry, _) => registry.UnregisterBlock(this)
		);
	}

	public void SetFusionActive(bool active)
	{
		_active = active;
		foreach (SpriteRenderer spriteRenderer in _activeRenderers)
		{
			spriteRenderer.enabled = _active;
		}

		var volatileBlock = GetComponent<VolatileBlock>();
		if (volatileBlock != null) volatileBlock.enabled = _active;
	}

	public IReadOnlyDictionary<string, float> GetGenerationRate()
	{
		return _active ? _generationRate : _empty;
	}

	public IReadOnlyDictionary<string, float> GetMaxGenerationRate()
	{
		return _generationRate;
	}
}
}