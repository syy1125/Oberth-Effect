using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.ControlCondition;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;
using Syy1125.OberthEffect.Spec.Unity;
using Syy1125.OberthEffect.Spec.Validation;
using UnityEngine;

namespace Syy1125.OberthEffect.CoreMod.Resource
{
[CreateSchemaFile("ResourceGeneratorSpecSchema")]
public class ResourceGeneratorSpec : ICustomValidation
{
	public Dictionary<string, float> ConsumptionRate;
	public Dictionary<string, float> GenerationRate;
	public ControlConditionSpec ActivationCondition;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public SoundReferenceSpec StartSound;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public SoundReferenceSpec StopSound;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public RendererSpec[] ActivationRenderers;

	public void Validate(List<string> path, List<string> errors)
	{
		ValidationHelper.ValidateFields(path, this, errors);
		path.Add(nameof(ConsumptionRate));
		ValidationHelper.ValidateResourceDictionary(path, ConsumptionRate, errors);
		path.RemoveAt(path.Count - 1);
		path.Add(nameof(GenerationRate));
		ValidationHelper.ValidateResourceDictionary(path, GenerationRate, errors);
		path.RemoveAt(path.Count - 1);
	}
}

public class ResourceGenerator : MonoBehaviour,
	IBlockComponent<ResourceGeneratorSpec>,
	IResourceConsumer,
	IResourceGenerator,
	IControlConditionReceiver,
	IHasDebrisState,
	ITooltipProvider
{
	public const string CLASS_KEY = "ResourceGenerator";

	private Dictionary<string, float> _consumptionRate;
	private Dictionary<string, float> _generationRate;
	private IControlCondition _activationCondition;
	private string _startSoundId;
	private AudioClip _startSound;
	private float _startSoundVolume;
	private string _stopSoundId;
	private AudioClip _stopSound;
	private float _stopSoundVolume;

	private Transform _activeRenderersParent;
	private AudioSource _audioSource;

	private bool _active = true;
	private float _satisfaction;

	private void OnEnable()
	{
		GetComponentInParent<IResourceGeneratorRegistry>()?.RegisterBlock(this);
		GetComponentInParent<IResourceConsumerRegistry>()?.RegisterBlock(this);
		GetComponentInParent<IControlConditionProvider>()?.RegisterBlock(this);
	}

	public void LoadSpec(ResourceGeneratorSpec spec, in BlockContext context)
	{
		_consumptionRate = spec.ConsumptionRate;
		_generationRate = spec.GenerationRate;
		_activationCondition = ControlConditionHelper.CreateControlCondition(spec.ActivationCondition);

		var provider = GetComponentInParent<IControlConditionProvider>();
		if (provider != null)
		{
			_active = provider.IsConditionTrue(_activationCondition);
		}

		if (spec.StartSound != null)
		{
			_startSoundId = spec.StartSound.SoundId;
			_startSound = SoundDatabase.Instance.GetAudioClip(_startSoundId);
			_startSoundVolume = spec.StartSound.Volume;
		}

		if (spec.StopSound != null)
		{
			_stopSoundId = spec.StopSound.SoundId;
			_stopSound = SoundDatabase.Instance.GetAudioClip(_stopSoundId);
			_stopSoundVolume = spec.StopSound.Volume;
		}

		if (_startSound != null || _stopSound != null)
		{
			_audioSource = SoundDatabase.Instance.CreateBlockAudioSource(gameObject, !context.IsMainVehicle);
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

		GetComponentInParent<IControlConditionProvider>()
			?
			.MarkControlGroupsActive(_activationCondition.GetControlGroups());
	}

	private void OnDisable()
	{
		GetComponentInParent<IResourceGeneratorRegistry>()?.UnregisterBlock(this);
		GetComponentInParent<IResourceConsumerRegistry>()?.UnregisterBlock(this);
		GetComponentInParent<IControlConditionProvider>()?.UnregisterBlock(this);
	}

	public void OnControlGroupsChanged(IControlConditionProvider provider)
	{
		bool active = provider.IsConditionTrue(_activationCondition);

		if (active != _active)
		{
			_active = active;

			if (_active && _startSound != null)
			{
				if (_audioSource.isPlaying) _audioSource.Stop();
				float volume = GetComponentInParent<IBlockSoundAttenuator>()
					.AttenuateOneShotSound(_startSoundId, _startSoundVolume);
				_audioSource.PlayOneShot(_startSound, volume);
			}
			else if (!_active && _stopSound != null)
			{
				if (_audioSource.isPlaying) _audioSource.Stop();
				float volume = GetComponentInParent<IBlockSoundAttenuator>()
					.AttenuateOneShotSound(_stopSoundId, _stopSoundVolume);
				_audioSource.PlayOneShot(_stopSound, volume);
			}

			if (_activeRenderersParent != null)
			{
				_activeRenderersParent.gameObject.SetActive(_active);
			}
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

	public float SatisfyResourceRequestAtLevel(float level)
	{
		_satisfaction = level;
		return _satisfaction;
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