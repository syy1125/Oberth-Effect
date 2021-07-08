using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Common;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Propulsion
{
public class OmniThruster : AbstractPropulsionBase, ITooltipProvider
{
	public ParticleSystem HorizontalParticles;
	public ParticleSystem VerticalParticles;

	private Vector2 _forwardBackResponse;
	private Vector2 _strafeResponse;
	private Vector2 _rotateResponse;
	private Vector2 _response;

	private Vector2 _maxParticleSpeed;

	protected override void Start()
	{
		base.Start();

		if (HorizontalParticles != null)
		{
			_maxParticleSpeed.x = HorizontalParticles.main.startSpeedMultiplier;

			if (Body != null) // Then we are in simulation
			{
				HorizontalParticles.Play();
			}
		}

		if (VerticalParticles != null)
		{
			_maxParticleSpeed.y = VerticalParticles.main.startSpeedMultiplier;

			if (Body != null)
			{
				VerticalParticles.Play();
			}
		}

		if (Body != null)
		{
			Vector3 localRight = Body.transform.InverseTransformDirection(transform.right);
			Vector3 localUp = Body.transform.InverseTransformDirection(transform.up);

			CalculateResponse(localRight, out _forwardBackResponse.x, out _strafeResponse.x, out _rotateResponse.x);
			CalculateResponse(localUp, out _forwardBackResponse.y, out _strafeResponse.y, out _rotateResponse.y);
		}
	}

	public override void SetPropulsionCommands(Vector2 translateCommand, float rotateCommand)
	{
		Vector2 rawResponse = _forwardBackResponse * translateCommand.y
		                      + _strafeResponse * translateCommand.x
		                      + _rotateResponse * rotateCommand;
		_response = new Vector2(
			Mathf.Clamp(rawResponse.x, -1f, 1f),
			Mathf.Clamp(rawResponse.y, -1f, 1f)
		);
	}

	public override IDictionary<VehicleResource, float> GetResourceConsumptionRateRequest()
	{
		float ratio = (Mathf.Abs(_response.x) + Mathf.Abs(_response.y)) / 2f;

		ResourceRequests.Clear();

		foreach (ResourceEntry entry in MaxResourceUse)
		{
			ResourceRequests.Add(entry.Resource, entry.Amount * ratio);
		}

		return ResourceRequests;
	}

	private void FixedUpdate()
	{
		Vector2 overallResponse = _response * Satisfaction;

		if (Body != null && IsMine)
		{
			Body.AddForceAtPosition(transform.TransformVector(overallResponse) * MaxForce, transform.position);
		}

		if (HorizontalParticles != null)
		{
			ParticleSystem.MainModule main = HorizontalParticles.main;
			main.startSpeedMultiplier = overallResponse.x * _maxParticleSpeed.x;
			main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, Mathf.Abs(overallResponse.x)));
		}

		if (VerticalParticles != null)
		{
			ParticleSystem.MainModule main = VerticalParticles.main;
			main.startSpeedMultiplier = overallResponse.y * _maxParticleSpeed.y;
			main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, Mathf.Abs(overallResponse.y)));
		}
	}

	public override float GetMaxPropulsionForce(CardinalDirection localDirection)
	{
		return MaxForce;
	}

	public string GetTooltip()
	{
		return string.Join(
			"\n",
			"Maneuvering thruster",
			"  Omni-directional",
			$"  Max thrust per direction {MaxForce * PhysicsConstants.KN_PER_UNIT_FORCE:#,0.#}kN",
			"  Max resource usage per direction "
			+ string.Join(
				" ",
				MaxResourceUse.Select(
					entry => $"{entry.RichTextColoredEntry()}/s"
				)
			)
		);
	}
}
}