using System.Collections.Generic;
using System.Text;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Foundation.Utils;
using Syy1125.OberthEffect.Spec.Block.Propulsion;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Propulsion
{
public class LinearEngine : AbstractThrusterBase, ITooltipProvider
{
	public const string CLASS_KEY = "LinearEngine";

	private float _throttleRate;
	private Vector2 _thrustOrigin;

	private string _thrustSoundId;
	private AudioSource _thrustSoundSource;
	private float _minVolume;
	private float _maxVolume;
	private ParticleSystemWrapper[] _particles;

	private float _forwardBackResponse;
	private float _strafeResponse;
	private float _rotateResponse;

	private float _targetThrustScale;
	private float _trueThrustScale;
	private Vector3 _localUp;

	public void LoadSpec(LinearEngineSpec spec)
	{
		MaxForce = spec.MaxForce;
		MaxResourceUse = spec.MaxResourceUse;
		ActivationCondition = ControlConditionHelper.CreateControlCondition(spec.ActivationCondition);
		_throttleRate = spec.MaxThrottleRate;
		_thrustOrigin = spec.ThrustOrigin;

		if (spec.ThrustSound != null)
		{
			_thrustSoundId = spec.ThrustSound.SoundId;
			_thrustSoundSource = gameObject.AddComponent<AudioSource>();
			_minVolume = spec.ThrustSound.MinVolume;
			_maxVolume = spec.ThrustSound.MaxVolume;

			_thrustSoundSource.clip = SoundDatabase.Instance.GetAudioClip(spec.ThrustSound.SoundId);
			_thrustSoundSource.volume = _minVolume;
			_thrustSoundSource.loop = true;
		}

		if (spec.Particles != null)
		{
			_particles = new ParticleSystemWrapper[spec.Particles.Length];

			for (var i = 0; i < spec.Particles.Length; i++)
			{
				_particles[i] = RendererHelper.CreateParticleSystem(transform, spec.Particles[i]);
			}
		}

		GetComponentInParent<IControlConditionProvider>()?
			.MarkControlGroupsActive(ActivationCondition.GetControlGroups());
	}

	protected override void Start()
	{
		base.Start();

		if (IsSimulation())
		{
			_localUp = Body.transform.InverseTransformDirection(transform.up);

			if (_thrustSoundSource != null)
			{
				_thrustSoundSource.Play();
			}

			if (_particles != null)
			{
				foreach (ParticleSystemWrapper particle in _particles)
				{
					particle.Play();
				}
			}
		}
	}

	public override void InitDefaultConfig()
	{
		RespondToTranslation = true;
		RespondToRotation = false;
	}

	protected override void SetPropulsionCommands(float horizontal, float vertical, float rotate)
	{
		if (!PropulsionActive)
		{
			_targetThrustScale = 0f;
			return;
		}

		CalculateResponse(_localUp, out _forwardBackResponse, out _strafeResponse, out _rotateResponse);

		float rawResponse = 0f;
		if (RespondToTranslation)
		{
			rawResponse += _forwardBackResponse * vertical + _strafeResponse * horizontal;
		}

		if (RespondToRotation)
		{
			rawResponse += _rotateResponse * rotate;
		}

		_targetThrustScale = Mathf.Clamp01(rawResponse);
	}

	public override IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest()
	{
		ResourceRequests.Clear();

		if (!Mathf.Approximately(_targetThrustScale, 0f))
		{
			foreach (KeyValuePair<string, float> entry in MaxResourceUse)
			{
				ResourceRequests.Add(entry.Key, entry.Value * _targetThrustScale);
			}
		}

		return ResourceRequests;
	}

	public override float SatisfyResourceRequestAtLevel(float level)
	{
		float maxSatisfaction = Mathf.Approximately(_targetThrustScale, 0f)
			? 0f
			: Mathf.Clamp01(_trueThrustScale + _throttleRate * Time.fixedDeltaTime) / _targetThrustScale;

		Satisfaction = Mathf.Min(level, maxSatisfaction);
		_trueThrustScale = _targetThrustScale * Satisfaction;

		return Satisfaction;
	}

	private void FixedUpdate()
	{
		if (IsSimulation())
		{
			if (IsMine)
			{
				Body.AddForceAtPosition(
					transform.up * (MaxForce * _trueThrustScale),
					transform.TransformPoint(_thrustOrigin)
				);
			}

			if (_thrustSoundSource != null)
			{
				float volume = Mathf.Lerp(_minVolume, _maxVolume, _trueThrustScale);
				_thrustSoundSource.volume = SoundAttenuator.AttenuatePersistentSound(_thrustSoundId, volume);
			}

			if (_particles != null)
			{
				ParticleSystemWrapper.BatchScaleThrustParticles(_particles, _trueThrustScale);
			}
		}
	}

	public override Vector2 GetPropulsionForceOrigin()
	{
		return _thrustOrigin;
	}

	public override float GetMaxPropulsionForce(CardinalDirection localDirection)
	{
		return localDirection == CardinalDirection.Up ? MaxForce : 0f;
	}

	public string GetTooltip()
	{
		StringBuilder builder = new StringBuilder();

		builder.AppendLine("Engine")
			.AppendLine($"  Max thrust {PhysicsUnitUtils.FormatForce(MaxForce)}");

		if (MaxResourceUse != null && MaxResourceUse.Count > 0)
		{
			builder.Append("  Max resource usage per second ")
				.AppendLine(string.Join(", ", VehicleResourceDatabase.Instance.FormatResourceDict(MaxResourceUse)));
		}

		builder.Append($"  Throttle response rate {_throttleRate * 100:F0}%/s");

		return builder.ToString();
	}
}
}