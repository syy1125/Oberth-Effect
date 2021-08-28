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

public class FusionGenerator : MonoBehaviour, IResourceGeneratorBlock, IVolatileComponent
{
	private Dictionary<string, float> _generationRate;
	private Dictionary<string, float> _empty = new Dictionary<string, float>();
	private bool _active;
	private Transform _activeRenderersParent;

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

		if (spec.ActivationRenderers != null)
		{
			_activeRenderersParent = new GameObject("ActiveRenderers").transform;
			_activeRenderersParent.SetParent(transform);
			_activeRenderersParent.localPosition = Vector3.back;
			_activeRenderersParent.localRotation = Quaternion.identity;
			_activeRenderersParent.localScale = Vector3.one;

			RendererHelper.AttachRenderers(_activeRenderersParent, spec.ActivationRenderers);
		}
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
		_activeRenderersParent.gameObject.SetActive(_active);
	}

	public float GetRadiusMultiplier()
	{
		return _active ? 1f : 0f;
	}

	public float GetDamageMultiplier()
	{
		return _active ? 1f : 0f;
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