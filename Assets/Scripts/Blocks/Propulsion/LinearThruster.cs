using System.Linq;
using Syy1125.OberthEffect.Common;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks
{
public class LinearThruster : MonoBehaviour, IPropulsionBlock
{
	public float MaxForce;
	public ResourceEntry[] MaxResourceUse;

	private Rigidbody2D _body;
	private ParticleSystem _particles;

	private float _forwardBackResponse;
	private float _strafeResponse;
	private float _rotateResponse;
	private float _maxParticleSpeed;

	private void Awake()
	{
		_body = GetComponentInParent<Rigidbody2D>();
		_particles = GetComponent<ParticleSystem>();
	}

	private void OnEnable()
	{
		ExecuteEvents.ExecuteHierarchy<IPropulsionBlockRegistry>(
			gameObject, null, (handler, _) => handler.RegisterBlock(this)
		);
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

	private void OnDisable()
	{
		ExecuteEvents.ExecuteHierarchy<IPropulsionBlockRegistry>(
			gameObject, null, (handler, _) => handler.UnregisterBlock(this)
		);
	}

	public PropulsionRequest GetResponse(float forwardBackCommand, float strafeCommand, float rotateCommand)
	{
		float rawResponse = _forwardBackResponse * forwardBackCommand
		                    + _strafeResponse * strafeCommand
		                    + _rotateResponse * rotateCommand;
		float response = Mathf.Clamp01(rawResponse);

		return new PropulsionRequest
		{
			ResourceConsumptionRateRequest = MaxResourceUse.ToDictionary(
				entry => entry.Resource, entry => entry.Amount * response
			),
			ForceOrigin = transform.position,
			Force = transform.up * (MaxForce * response)
		};
	}

	public void PlayEffect(PropulsionRequest request, float satisfactionLevel)
	{
		{
			float response = Mathf.Clamp01(request.Force.magnitude / MaxForce);
			float strength = satisfactionLevel * response;

			ParticleSystem.MainModule main = _particles.main;
			main.startSpeedMultiplier = strength * _maxParticleSpeed;
			main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, strength));
		}
	}
}
}