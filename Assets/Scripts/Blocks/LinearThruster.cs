using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Common;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
public struct ThrusterResponse
{
	public Dictionary<VehicleResource, float> ResourceConsumptionRateRequest;
	public Vector3 ForceOrigin;
	public Vector3 Force;
}

public class LinearThruster : MonoBehaviour
{
	public float MaxForce;
	public ResourceEntry[] MaxResourceUse;

	private Rigidbody2D _body;
	private ParticleSystem _particles;

	private float _forwardBackResponse;
	private float _strafeResponse;
	private float _rotateResponse;
	private float _maxParticleSpeed;
	private float _response;

	private void Awake()
	{
		_body = GetComponentInParent<Rigidbody2D>();
		_particles = GetComponent<ParticleSystem>();
	}

	private void Start()
	{
		if (_body != null)
		{
			Vector3 localUp = transform.localRotation * Vector3.up;
			Vector3 localPosition = transform.localPosition - (Vector3) _body.centerOfMass;

			_forwardBackResponse = localUp.y;
			_strafeResponse = localUp.x;
			_rotateResponse = localUp.x * localPosition.y - localUp.y * localPosition.x;

			_rotateResponse = Mathf.Abs(_rotateResponse) > 1e-5 ? Mathf.Sign(_rotateResponse) : 0f;
		}

		if (_particles != null)
		{
			_maxParticleSpeed = _particles.main.startSpeedMultiplier;
			_particles.Play();
		}
	}

	public void SetCommands(float forwardBackCommand, float strafeCommand, float rotateCommand)
	{
		float rawResponse = _forwardBackResponse * forwardBackCommand
		                    + _strafeResponse * strafeCommand
		                    + _rotateResponse * rotateCommand;
		_response = Mathf.Clamp01(rawResponse);
	}

	public ThrusterResponse GetResponse()
	{
		return new ThrusterResponse
		{
			ResourceConsumptionRateRequest = MaxResourceUse.ToDictionary(
				entry => entry.Resource, entry => entry.Amount * _response
			),
			ForceOrigin = transform.position,
			Force = transform.up * (MaxForce * _response)
		};
	}

	public void PlayEffect(float satisfactionLevel)
	{
		float strength = satisfactionLevel * _response;

		if (_particles != null)
		{
			ParticleSystem.MainModule main = _particles.main;
			main.startSpeedMultiplier = strength * _maxParticleSpeed;
			main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, strength));
		}
	}
}
}