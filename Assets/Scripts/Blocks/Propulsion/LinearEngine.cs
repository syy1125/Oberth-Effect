using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Syy1125.OberthEffect.Common;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Propulsion
{
public class LinearEngine : AbstractPropulsionBase, ITooltipProvider, IConfigComponent
{
	public const string CONFIG_KEY = "LinearEngine";

	public float MaxThrottleRate;

	public bool RespondToTranslation;
	public bool RespondToRotation;

	private ParticleSystem _particles;

	private float _forwardBackResponse;
	private float _strafeResponse;
	private float _rotateResponse;

	private float _targetThrustStrength;
	private float _trueThrustStrength;

	private float _maxParticleSpeed;

	protected override void Awake()
	{
		base.Awake();

		_particles = GetComponent<ParticleSystem>();
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
				_maxParticleSpeed = _particles.main.startSpeedMultiplier;
				_particles.Play();
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

		_targetThrustStrength = Mathf.Clamp01(
			Mathf.Clamp(
				rawResponse,
				_trueThrustStrength - MaxThrottleRate * Time.fixedDeltaTime,
				_trueThrustStrength + MaxThrottleRate * Time.fixedDeltaTime
			)
		);
	}

	public override IDictionary<VehicleResource, float> GetResourceConsumptionRateRequest()
	{
		ResourceRequests.Clear();

		foreach (ResourceEntry entry in MaxResourceUse)
		{
			ResourceRequests.Add(entry.Resource, entry.Amount * _targetThrustStrength);
		}

		return ResourceRequests;
	}

	private void FixedUpdate()
	{
		_trueThrustStrength = _targetThrustStrength * Satisfaction;

		if (Body != null && IsMine)
		{
			Body.AddForceAtPosition(transform.up * (MaxForce * _trueThrustStrength), transform.position);
		}

		if (_particles != null)
		{
			ParticleSystem.MainModule main = _particles.main;
			main.startSpeedMultiplier = _trueThrustStrength * _maxParticleSpeed;
			main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, _trueThrustStrength));
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
			"  Max resource usage "
			+ string.Join(
				" ",
				MaxResourceUse.Select(
					entry => $"{entry.RichTextColoredEntry()}/s"
				)
			),
			$"  Throttle response rate {MaxThrottleRate * 100:F0}%/s"
		);
	}
}
}