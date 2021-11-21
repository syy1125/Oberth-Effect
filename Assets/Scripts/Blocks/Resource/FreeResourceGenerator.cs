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
public class FreeResourceGenerator : MonoBehaviour, IResourceGeneratorBlock, IControlConditionReceiver, IHasDebrisLogic,
	ITooltipProvider
{
	private Dictionary<string, float> _generationRate;
	private ControlConditionSpec _activationCondition;
	private Transform _activeRenderersParent;
	private bool _active = true;

	private void OnEnable()
	{
		GetComponentInParent<IResourceGeneratorBlockRegistry>()?.RegisterBlock(this);
		GetComponentInParent<IControlConditionProvider>()?.RegisterBlock(this);
	}

	public void LoadSpec(FreeGeneratorSpec spec)
	{
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
		GetComponentInParent<IResourceGeneratorBlockRegistry>()?.UnregisterBlock(this);
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

	public void EnterDebrisMode()
	{
		if (_activeRenderersParent != null)
		{
			_activeRenderersParent.gameObject.SetActive(false);
		}
	}

	public IReadOnlyDictionary<string, float> GetGenerationRate()
	{
		return _active ? _generationRate : null;
	}

	public string GetTooltip()
	{
		return "Passive resource generation\n  "
		       + string.Join(
			       ", ",
			       VehicleResourceDatabase.Instance.FormatResourceDict(_generationRate)
				       .Select(entry => $"{entry}/s")
		       );
	}

	public IReadOnlyDictionary<string, float> GetMaxGenerationRate()
	{
		return _generationRate;
	}
}
}