using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Spec.Block.Propulsion;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Propulsion
{
public class LinearEngine : AbstractPropulsionBase, ITooltipProvider, IConfigComponent
{
	public const string CONFIG_KEY = "LinearEngine";

	private float _maxThrottleRate;

	[NonSerialized]
	public bool RespondToTranslation;
	[NonSerialized]
	public bool RespondToRotation;

	private ParticleSystem[] _particles;
	private float[] _maxParticleSpeeds;

	private float _forwardBackResponse;
	private float _strafeResponse;
	private float _rotateResponse;

	private float _targetThrustStrength;

	public void LoadSpec(LinearEngineSpec spec)
	{
		MaxForce = spec.MaxForce;
		MaxResourceUse = spec.MaxResourceUse;
		IsFuelPropulsion = spec.IsFuelPropulsion;
		_maxThrottleRate = spec.MaxThrottleRate;

		if (spec.Particles != null)
		{
			_particles = new ParticleSystem[spec.Particles.Length];
			_maxParticleSpeeds = new float[spec.Particles.Length];

			for (var i = 0; i < spec.Particles.Length; i++)
			{
				_particles[i] = CreateParticleSystem(transform, spec.Particles[i]);
				_maxParticleSpeeds[i] = spec.Particles[i].MaxSpeed;
			}
		}
	}

	protected override void Start()
	{
		base.Start();

		if (Body != null)
		{
			// We are in simulation
			Vector3 localUp = MassContext.transform.InverseTransformDirection(transform.up);
			CalculateResponse(localUp, out _forwardBackResponse, out _strafeResponse, out _rotateResponse);

			if (_particles != null)
			{
				// We have particles
				foreach (ParticleSystem particle in _particles)
				{
					particle.Play();
				}
			}
		}
	}

	public JObject ExportConfig()
	{
		return new JObject
		{
			{ "RespondToTranslation", new JValue(RespondToTranslation) },
			{ "RespondToRotation", new JValue(RespondToRotation) }
		};
	}

	public void InitDefaultConfig()
	{
		RespondToTranslation = true;
		RespondToRotation = false;
	}

	public void ImportConfig(JObject config)
	{
		if (config.ContainsKey("RespondToTranslation"))
		{
			RespondToTranslation = config["RespondToTranslation"].Value<bool>();
		}

		if (config.ContainsKey("RespondToRotation"))
		{
			RespondToRotation = config["RespondToRotation"].Value<bool>();
		}
	}

	public override void SetPropulsionCommands(Vector2 translateCommand, float rotateCommand)
	{
		if (IsFuelPropulsion && !FuelPropulsionActive)
		{
			_targetThrustStrength = 0f;
			return;
		}

		float rawResponse = 0f;
		if (RespondToTranslation)
		{
			rawResponse += _forwardBackResponse * translateCommand.y + _strafeResponse * translateCommand.x;
		}

		if (RespondToRotation)
		{
			rawResponse += _rotateResponse * rotateCommand;
		}

		float trueThrustStrength = _targetThrustStrength * Satisfaction;
		_targetThrustStrength = Mathf.Clamp01(
			Mathf.Clamp(
				rawResponse,
				trueThrustStrength - _maxThrottleRate * Time.fixedDeltaTime,
				trueThrustStrength + _maxThrottleRate * Time.fixedDeltaTime
			)
		);
	}

	public override IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest()
	{
		ResourceRequests.Clear();

		foreach (KeyValuePair<string, float> entry in MaxResourceUse)
		{
			ResourceRequests.Add(entry.Key, entry.Value * _targetThrustStrength);
		}

		return ResourceRequests;
	}

	private void FixedUpdate()
	{
		float trueThrustStrength = _targetThrustStrength * Satisfaction;

		if (Body != null && IsMine)
		{
			Body.AddForceAtPosition(transform.up * (MaxForce * trueThrustStrength), transform.position);
		}

		if (_particles != null)
		{
			for (var i = 0; i < _particles.Length; i++)
			{
				ParticleSystem.MainModule main = _particles[i].main;
				main.startSpeedMultiplier = trueThrustStrength * _maxParticleSpeeds[i];
				Color startColor = main.startColor.color;
				startColor.a = trueThrustStrength;
				main.startColor = new ParticleSystem.MinMaxGradient(startColor);
			}
		}
	}

	public override float GetMaxPropulsionForce(CardinalDirection localDirection)
	{
		return localDirection == CardinalDirection.Up ? MaxForce : 0f;
	}

	public string GetTooltip()
	{
		return string.Join(
			"\n",
			"Engine",
			$"  Max thrust {MaxForce * PhysicsConstants.KN_PER_UNIT_FORCE:#,0.#}kN",
			"  Max resource usage per second "
			+ string.Join(
				", ",
				VehicleResourceDatabase.Instance.FormatResourceDict(MaxResourceUse)
			),
			$"  Throttle response rate {_maxThrottleRate * 100:F0}%/s"
		);
	}
}
}