using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Spec.Block.Resource;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Resource
{
public class FreeResourceGenerator : MonoBehaviour, IResourceGeneratorBlock, IControlConditionReceiver, ITooltipProvider
{
	private Dictionary<string, float> _generationRate;
	private ControlConditionSpec _activationCondition;
	private Transform _activeRenderersParent;
	private bool _active = true;

	private void OnEnable()
	{
		ExecuteEvents.ExecuteHierarchy<IResourceGeneratorBlockRegistry>(
			gameObject, null, (handler, _) => handler.RegisterBlock(this)
		);
		ExecuteEvents.ExecuteHierarchy<IControlConditionProvider>(
			gameObject, null, (handler, _) => handler.RegisterBlock(this)
		);
	}

	public void LoadSpec(FreeGeneratorSpec spec)
	{
		_generationRate = spec.GenerationRate;
		_activationCondition = spec.ActivationCondition;

		var provider = ComponentUtils.GetBehaviourInParent<IControlConditionProvider>(transform);
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
		ExecuteEvents.ExecuteHierarchy<IResourceGeneratorBlockRegistry>(
			gameObject, null, (handler, _) => handler.UnregisterBlock(this)
		);
		ExecuteEvents.ExecuteHierarchy<IControlConditionProvider>(
			gameObject, null, (handler, _) => handler.UnregisterBlock(this)
		);
	}

	public void OnControlGroupsChanged(IControlConditionProvider provider)
	{
		_active = provider.IsConditionTrue(_activationCondition);

		if (_activeRenderersParent != null)
		{
			_activeRenderersParent.gameObject.SetActive(_active);
		}
	}

	public IReadOnlyDictionary<string, float> GetGenerationRate()
	{
		return _active ? _generationRate : null;
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