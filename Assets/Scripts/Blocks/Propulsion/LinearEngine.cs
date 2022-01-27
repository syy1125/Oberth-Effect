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

		if (Body != null)
		{
			// We are in simulation
			_localUp = Body.transform.InverseTransformDirection(transform.up);

			if (_particles != null)
			{
				// We have particles
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
		float trueThrustScale = _targetThrustScale * Satisfaction;

		if (Body != null && IsMine)
		{
			Body.AddForceAtPosition(transform.up * (MaxForce * trueThrustScale), transform.position);
		}

		if (_particles != null)
		{
			ParticleSystemWrapper.BatchScaleThrustParticles(_particles, trueThrustScale);
		}
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