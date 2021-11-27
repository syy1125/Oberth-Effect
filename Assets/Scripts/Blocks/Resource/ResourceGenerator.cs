using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Syy1125.OberthEffect.Spec.Block.Resource;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Resource
{
public class ResourceGenerator :
	MonoBehaviour,
	IResourceConsumer, IResourceGenerator,
	IControlConditionReceiver, IHasDebrisState, ITooltipProvider
{
	public const string CLASS_KEY = "ResourceGenerator";

	private Dictionary<string, float> _consumptionRate;
	private Dictionary<string, float> _generationRate;
	private ControlConditionSpec _activationCondition;
	private Transform _activeRenderersParent;

	private bool _active = true;
	private float _satisfaction;

	private void OnEnable()
	{
		GetComponentInParent<IResourceGeneratorRegistry>()?.RegisterBlock(this);
		GetComponentInParent<IResourceConsumerRegistry>()?.RegisterBlock(this);
		GetComponentInParent<IControlConditionProvider>()?.RegisterBlock(this);
	}

	public void LoadSpec(ResourceGeneratorSpec spec)
	{
		_consumptionRate = spec.ConsumptionRate;
		_generationRate = spec.GenerationRate;
		_activationCondition = spec.ActivationCondition;

		var provider = GetComponentInParent<IControlConditionProvider>();
		if (provider != null)
		{
			_active = provider.IsConditionTrue(_activationCondition);
		}

		if (spec.ActivationRenderers != null)
		{
			_activeRenderersParent = new GameObject("ActiveRenderers").transform;
			_activeRenderersParent.SetParent(transform);
			_activeRenderersParent.localPosition = Vector3.back;
			_activeRenderersParent.localRotation = Quaternion.identity;
			_activeRenderersParent.localScale = Vector3.one;

			RendererHelper.AttachRenderers(_activeRenderersParent, spec.ActivationRenderers);

			_activeRenderersParent.gameObject.SetActive(_active);
		}
	}

	private void OnDisable()
	{
		GetComponentInParent<IResourceGeneratorRegistry>()?.UnregisterBlock(this);
		GetComponentInParent<IResourceConsumerRegistry>()?.UnregisterBlock(this);
		GetComponentInParent<IControlConditionProvider>()?.UnregisterBlock(this);
	}

	public void OnControlGroupsChanged(IControlConditionProvider provider)
	{
		_active = provider.IsConditionTrue(_activationCondition);

		if (_activeRenderersParent != null)
		{
			_activeRenderersParent.gameObject.SetActive(_active);
		}
	}

	public JObject SaveDebrisState()
	{
		return null;
	}

	public void LoadDebrisState(JObject state)
	{
		if (_activeRenderersParent != null)
		{
			_activeRenderersParent.gameObject.SetActive(false);
		}
	}

	public IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest()
	{
		return _active ? _consumptionRate : null;
	}

	public void SatisfyResourceRequestAtLevel(float level)
	{
		_satisfaction = level;
	}

	public IReadOnlyDictionary<string, float> GetGenerationRate()
	{
		if (!_active)
		{
			return null;
		}

		if (_consumptionRate == null || _consumptionRate.Count == 0)
		{
			return _generationRate;
		}
		else
		{
			return _generationRate.ToDictionary(
				entry => entry.Key,
				entry => entry.Value * _satisfaction
			);
		}
	}

	public string GetTooltip()
	{
		if (_consumptionRate == null || _consumptionRate.Count == 0)
		{
			return "Resource generation\n  "
			       + string.Join(
				       ", ",
				       VehicleResourceDatabase.Instance.FormatResourceDict(_generationRate)
					       .Select(entry => $"{entry}/s")
			       );
		}
		else
		{
			return "Resource converter\n  Max consumption "
			       + string.Join(
				       ", ",
				       VehicleResourceDatabase.Instance.FormatResourceDict(_consumptionRate)
					       .Select(entry => $"{entry}/s")
			       )
			       + "\n  Max production "
			       + string.Join(
				       ", ",
				       VehicleResourceDatabase.Instance.FormatResourceDict(_generationRate)
					       .Select(entry => $"{entry}/s")
			       );
		}
	}

	public IReadOnlyDictionary<string, float> GetMaxGenerationRate()
	{
		return _generationRate;
	}

	public IReadOnlyDictionary<string, float> GetMaxResourceUseRate()
	{
		return _consumptionRate;
	}
}
}