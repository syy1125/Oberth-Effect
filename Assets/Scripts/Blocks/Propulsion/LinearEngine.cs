using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Syy1125.OberthEffect.Blocks.Config;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Spec.Block.Propulsion;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Propulsion
{
public class LinearEngine : AbstractPropulsionBase, ITooltipProvider, IConfigComponent
{
	public const string CLASS_KEY = "LinearEngine";

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
	private Vector3 _localUp;

	public void LoadSpec(LinearEngineSpec spec)
	{
		MaxForce = spec.MaxForce;
		MaxResourceUse = spec.MaxResourceUse;
		ActivationCondition = ControlConditionHelper.CreateControlCondition(spec.ActivationCondition);
		_maxThrottleRate = spec.MaxThrottleRate;

		if (spec.Particles != null)
		{
			_particles = new ParticleSystem[spec.Particles.Length];
			_maxParticleSpeeds = new float[spec.Particles.Length];

			for (var i = 0; i < spec.Particles.Length; i++)
			{
				_particles[i] = RendererHelper.CreateParticleSystem(transform, spec.Particles[i]);
				_maxParticleSpeeds[i] = spec.Particles[i].MaxSpeed;
			}
		}

		GetComponentInParent<IControlConditionProvider>()?
			.MarkControlGroupsActive(ActivationCondition.GetControlGroups());
	}

	protected override void Start()
	{
		base.Start();

		if (Body != null)
		{
			// We are in simulation
			_localUp = Body.transform.InverseTransformDirection(transform.up);

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

	#region Config

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

	public List<ConfigItemBase> GetConfigItems()
	{
		return new List<ConfigItemBase>
		{
			new ToggleConfigItem
			{
				Key = "RespondToTranslation",
				Label = "Respond to translation"
			},
			new ToggleConfigItem
			{
				Key = "RespondToRotation",
				Label = "Respond to rotation"
			}
		};
	}

	#endregion

	protected override void SetPropulsionCommands(float horizontal, float vertical, float rotate)
	{
		if (!PropulsionActive)
		{
			_targetThrustStrength = 0f;
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

		float trueThrustStrength = _targetThrustStrength * Satisfaction;
		_targetThrustStrength = Mathf.Clamp01(
			Mathf.Min(rawResponse, trueThrustStrength + _maxThrottleRate * Time.fixedDeltaTime)
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
			$"  Max thrust {PhysicsUnitUtils.FormatForce(MaxForce)}",
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