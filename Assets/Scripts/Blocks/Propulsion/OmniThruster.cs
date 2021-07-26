using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Spec.Block.Propulsion;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Propulsion
{
public class OmniThruster : AbstractPropulsionBase, ITooltipProvider
{
	private Transform _verticalParticleRoot;
	private ParticleSystem[] _verticalParticles;
	private Transform _horizontalParticleRoot;
	private ParticleSystem[] _horizontalParticles;
	private float[] _maxParticleSpeeds;

	private Vector2 _forwardBackResponse;
	private Vector2 _strafeResponse;
	private Vector2 _rotateResponse;
	private Vector2 _response;

	public void LoadSpec(OmniThrusterSpec spec)
	{
		MaxForce = spec.MaxForce;
		MaxResourceUse = spec.MaxResourceUse;
		IsFuelPropulsion = spec.IsFuelPropulsion;

		if (spec.Particles != null)
		{
			int particleCount = spec.Particles.Length;

			_verticalParticleRoot = new GameObject("VerticalParticles").transform;
			_verticalParticleRoot.SetParent(transform);
			_verticalParticles = new ParticleSystem[particleCount];

			_horizontalParticleRoot = new GameObject("HorizontalParticles").transform;
			_horizontalParticleRoot.SetParent(transform);
			_horizontalParticleRoot.rotation = Quaternion.AngleAxis(90f, Vector3.forward);
			_horizontalParticles = new ParticleSystem[particleCount];

			_maxParticleSpeeds = new float[particleCount];

			for (int i = 0; i < particleCount; i++)
			{
				_verticalParticles[i] = CreateParticleSystem(_verticalParticleRoot, spec.Particles[i]);
				_horizontalParticles[i] = CreateParticleSystem(_horizontalParticleRoot, spec.Particles[i]);
				_maxParticleSpeeds[i] = spec.Particles[i].MaxSpeed;
			}
		}
	}

	protected override void Start()
	{
		base.Start();

		if (Body != null)
		{
			Vector3 localRight = Body.transform.InverseTransformDirection(transform.right);
			Vector3 localUp = Body.transform.InverseTransformDirection(transform.up);

			CalculateResponse(localRight, out _forwardBackResponse.x, out _strafeResponse.x, out _rotateResponse.x);
			CalculateResponse(localUp, out _forwardBackResponse.y, out _strafeResponse.y, out _rotateResponse.y);

			if (_horizontalParticles != null)
			{
				foreach (ParticleSystem particle in _horizontalParticles)
				{
					particle.Play();
				}
			}

			if (_verticalParticles != null)
			{
				foreach (ParticleSystem particle in _verticalParticles)
				{
					particle.Play();
				}
			}
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

	public override IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest()
	{
		float ratio = (Mathf.Abs(_response.x) + Mathf.Abs(_response.y)) / 2f;

		ResourceRequests.Clear();

		foreach (KeyValuePair<string, float> entry in MaxResourceUse)
		{
			ResourceRequests.Add(entry.Key, entry.Value * ratio);
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

		if (_horizontalParticleRoot != null && _horizontalParticles != null)
		{
			_horizontalParticleRoot.localRotation = Quaternion.AngleAxis(
				overallResponse.x > 0 ? -90f : 90f, Vector3.forward
			);

			for (int i = 0; i < _horizontalParticles.Length; i++)
			{
				ParticleSystem.MainModule main = _horizontalParticles[i].main;
				main.startSpeedMultiplier = overallResponse.x * _maxParticleSpeeds[i];
				var startColor = main.startColor.color;
				startColor.a = Mathf.Abs(overallResponse.x);
				main.startColor = startColor;
			}
		}

		if (_verticalParticleRoot != null && _verticalParticles != null)
		{
			_verticalParticleRoot.localRotation = Quaternion.AngleAxis(
				overallResponse.y > 0 ? 0f : 180f, Vector3.forward
			);

			for (int i = 0; i < _verticalParticles.Length; i++)
			{
				ParticleSystem.MainModule main = _verticalParticles[i].main;
				main.startSpeedMultiplier = overallResponse.y * _maxParticleSpeeds[i];
				var startColor = main.startColor.color;
				startColor.a = Mathf.Abs(overallResponse.y);
				main.startColor = startColor;
			}
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
			"  Max resource usage per second "
			+ string.Join(
				", ",
				VehicleResourceDatabase.Instance.FormatResourceDict(MaxResourceUse)
			)
		);
	}
}
}