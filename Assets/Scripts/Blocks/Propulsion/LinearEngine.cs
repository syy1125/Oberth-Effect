using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Common;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Propulsion
{
public class LinearEngine : AbstractPropulsionBase, ITooltipProvider
{
	public float MaxThrottleRate;

	private ParticleSystem _particles;

	private float _forwardBackResponse;
	private float _strafeResponse;
	private float _rotateResponse;
	private float _response;

	private float _maxParticleSpeed;

	protected override void Awake()
	{
		base.Awake();

		_particles = GetComponent<ParticleSystem>();
	}


	protected override void Start()
	{
		base.Start();

		if (_particles != null)
		{
			_maxParticleSpeed = _particles.main.startSpeedMultiplier;

			if (Body != null) // Then we are in simulation
			{
				_particles.Play();
			}
		}

		if (Body != null)
		{
			Vector3 localUp = transform.localRotation * Vector3.up;
			CalculateResponse(localUp, out _forwardBackResponse, out _strafeResponse, out _rotateResponse);
		}
	}


	public override void SetPropulsionCommands(float forwardBackCommand, float strafeCommand, float rotateCommand)
	{
		float rawResponse = _forwardBackResponse * forwardBackCommand
		                    + _strafeResponse * strafeCommand
		                    + _rotateResponse * rotateCommand;
		_response = Mathf.Clamp01(
			Mathf.Clamp(
				rawResponse,
				_response - MaxThrottleRate * Time.fixedDeltaTime,
				_response + MaxThrottleRate * Time.fixedDeltaTime
			)
		);
	}

	public override IDictionary<VehicleResource, float> GetResourceConsumptionRateRequest()
	{
		ResourceRequests.Clear();

		foreach (ResourceEntry entry in MaxResourceUse)
		{
			ResourceRequests.Add(entry.Resource, entry.Amount * _response);
		}

		return ResourceRequests;
	}

	private void FixedUpdate()
	{
		float overallResponse = _response * Satisfaction;

		if (Body != null && IsMine)
		{
			Body.AddForceAtPosition(transform.up * overallResponse, transform.position);
		}

		if (_particles != null)
		{
			ParticleSystem.MainModule main = _particles.main;
			main.startSpeedMultiplier = overallResponse * _maxParticleSpeed;
			main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, overallResponse));
		}
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