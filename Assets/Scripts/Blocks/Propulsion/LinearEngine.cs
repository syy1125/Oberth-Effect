using System.Collections.Generic;
using System.Text;
using Syy1125.OberthEffect.Blocks.Config;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Common.Utils;
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

	private float _maxThrottleRate;
	private Vector2 _thrustOrigin;

	private AudioSource _thrustSound;
	private float _minVolume;
	private float _maxVolume;
	private ParticleSystemWrapper[] _particles;

	private float _forwardBackResponse;
	private float _strafeResponse;
	private float _rotateResponse;

	private float _targetThrustScale;
	private Vector3 _localUp;

	public void LoadSpec(LinearEngineSpec spec)
	{
		MaxForce = spec.MaxForce;
		MaxResourceUse = spec.MaxResourceUse;
		ActivationCondition = ControlConditionHelper.CreateControlCondition(spec.ActivationCondition);
		_maxThrottleRate = spec.MaxThrottleRate;
		_thrustOrigin = spec.ThrustOrigin;

		if (spec.ThrustSound != null)
		{
			_thrustSound = gameObject.AddComponent<AudioSource>();
			_minVolume = spec.ThrustSound.MinVolume;
			_maxVolume = spec.ThrustSound.MaxVolume;

			_thrustSound.clip = SoundDatabase.Instance.GetAudioClip(spec.ThrustSound.SoundId);
			_thrustSound.volume = _minVolume;
			_thrustSound.loop = true;
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

			if (_thrustSound != null)
			{
				_thrustSound.Play();
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

		float trueThrustStrength = _targetThrustScale * Satisfaction;
		_targetThrustScale = Mathf.Clamp01(
			Mathf.Min(rawResponse, trueThrustStrength + _maxThrottleRate * Time.fixedDeltaTime)
		);
	}

	public override IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest()
	{
		ResourceRequests.Clear();

		foreach (KeyValuePair<string, float> entry in MaxResourceUse)
		{
			ResourceRequests.Add(entry.Key, entry.Value * _targetThrustScale);
		}

		return ResourceRequests;
	}

	private void FixedUpdate()
	{
		if (IsSimulation())
		{
			float trueThrustScale = _targetThrustScale * Satisfaction;

			if (IsMine)
			{
				Body.AddForceAtPosition(
					transform.up * (MaxForce * trueThrustScale),
					transform.TransformPoint(_thrustOrigin)
				);
			}

			if (_thrustSound != null)
			{
				_thrustSound.volume = Mathf.Lerp(_minVolume, _maxVolume, trueThrustScale);
			}

			if (_particles != null)
			{
				ParticleSystemWrapper.BatchScaleThrustParticles(_particles, trueThrustScale);
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

		builder.Append($"  Throttle response rate {_maxThrottleRate * 100:F0}%/s");

		return builder.ToString();
	}
}
}