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

	private ParticleSystem _particles;

	[HideInInspector]
	public float ForwardBackResponse;
	[HideInInspector]
	public float StrafeResponse;
	[HideInInspector]
	public float RotateResponse;

	private float _targetThrustStrength;

	private float _maxParticleSpeed;

	protected override void Awake()
	{
		base.Awake();

		_particles = GetComponent<ParticleSystem>();
	}


	protected override void Start()
	{
		base.Start();

		if (Body != null && _particles != null)
		{
			// We are in simulation and we have particles
			_maxParticleSpeed = _particles.main.startSpeedMultiplier;
			_particles.Play();
		}
	}

	public JObject ExportConfig()
	{
		return new JObject
		{
			{ "ForwardBackResponse", new JValue(ForwardBackResponse) },
			{ "StrafeResponse", new JValue(StrafeResponse) },
			{ "RotateResponse", new JValue(RotateResponse) }
		};
	}

	public void InitDefaultConfig()
	{
		Vector3 localUp = MassContext.transform.InverseTransformDirection(transform.up);
		// Engines ignore rotation commands
		CalculateResponse(localUp, out ForwardBackResponse, out StrafeResponse, out float _);
		RotateResponse = 0f;
	}

	public void ImportConfig(JObject config)
	{
		if (config.ContainsKey("ForwardBackResponse"))
		{
			ForwardBackResponse = config["ForwardBackResponse"].ToObject<float>();
		}

		if (config.ContainsKey("StrafeResponse"))
		{
			StrafeResponse = config["StrafeResponse"].ToObject<float>();
		}

		if (config.ContainsKey("RotateResponse"))
		{
			RotateResponse = config["RotateResponse"].ToObject<float>();
		}
	}

	public override void SetPropulsionCommands(float forwardBackCommand, float strafeCommand, float rotateCommand)
	{
		float rawResponse = ForwardBackResponse * forwardBackCommand
		                    + StrafeResponse * strafeCommand
		                    + RotateResponse * rotateCommand;
		_targetThrustStrength = Mathf.Clamp01(
			Mathf.Clamp(
				rawResponse,
				_targetThrustStrength - MaxThrottleRate * Time.fixedDeltaTime,
				_targetThrustStrength + MaxThrottleRate * Time.fixedDeltaTime
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
		float overallResponse = _targetThrustStrength * Satisfaction;

		if (Body != null && IsMine)
		{
			Body.AddForceAtPosition(transform.up * (MaxForce * overallResponse), transform.position);
		}

		if (_particles != null)
		{
			ParticleSystem.MainModule main = _particles.main;
			main.startSpeedMultiplier = overallResponse * _maxParticleSpeed;
			main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, overallResponse));
		}
	}

	public override float GetMaxPropulsionForce(CardinalDirection localDirection)
	{
		return localDirection == CardinalDirection.Down ? MaxForce : 0f;
	}

	public string GetTooltip()
	{
		return string.Join(
			"\n",
			"Engine",
			$"  Max thrust {MaxForce} kN",
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