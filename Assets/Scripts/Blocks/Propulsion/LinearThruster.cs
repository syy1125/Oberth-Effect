using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.Common;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Propulsion
{
public class LinearThruster : MonoBehaviour, IPropulsionBlock, IResourceConsumerBlock, ITooltipProvider
{
	public float MaxForce;
	public ResourceEntry[] MaxResourceUse;

	private Rigidbody2D _body;
	private ParticleSystem _particles;
	private bool _isMine;

	private float _forwardBackResponse;
	private float _strafeResponse;
	private float _rotateResponse;
	private float _response;

	private Dictionary<VehicleResource, float> _resourceRequests;
	private float _satisfaction;

	private float _maxParticleSpeed;

	private void Awake()
	{
		_body = GetComponentInParent<Rigidbody2D>();
		_particles = GetComponent<ParticleSystem>();

		_resourceRequests = new Dictionary<VehicleResource, float>();
	}

	private void OnEnable()
	{
		ExecuteEvents.ExecuteHierarchy<IPropulsionBlockRegistry>(
			gameObject, null, (handler, _) => handler.RegisterBlock(this)
		);
		ExecuteEvents.ExecuteHierarchy<IResourceConsumerBlockRegistry>(
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

		var photonView = GetComponentInParent<PhotonView>();
		_isMine = photonView == null || photonView.IsMine;
	}

	private void OnDisable()
	{
		ExecuteEvents.ExecuteHierarchy<IPropulsionBlockRegistry>(
			gameObject, null, (handler, _) => handler.UnregisterBlock(this)
		);
		ExecuteEvents.ExecuteHierarchy<IResourceConsumerBlockRegistry>(
			gameObject, null, (handler, _) => handler.UnregisterBlock(this)
		);
	}

	public void SetPropulsionCommands(float forwardBackCommand, float strafeCommand, float rotateCommand)
	{
		float rawResponse = _forwardBackResponse * forwardBackCommand
		                    + _strafeResponse * strafeCommand
		                    + _rotateResponse * rotateCommand;
		_response = Mathf.Clamp01(rawResponse);
	}

	public IDictionary<VehicleResource, float> GetResourceConsumptionRateRequest()
	{
		_resourceRequests.Clear();

		foreach (ResourceEntry entry in MaxResourceUse)
		{
			_resourceRequests.Add(entry.Resource, entry.Amount * _response);
		}

		return _resourceRequests;
	}

	public void SatisfyResourceRequestAtLevel(float level)
	{
		_satisfaction = level;
	}

	private void FixedUpdate()
	{
		float overallResponse = _response * _satisfaction;

		if (_body != null && _isMine)
		{
			_body.AddForceAtPosition(transform.up * overallResponse, transform.position);
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
			)
		);
	}
}
}