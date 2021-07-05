using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.Common;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Propulsion
{
public abstract class AbstractPropulsionBase : MonoBehaviour, IPropulsionBlock, IResourceConsumerBlock
{
	public float MaxForce;
	public ResourceEntry[] MaxResourceUse;

	protected Rigidbody2D Body;
	protected bool IsMine;


	protected Dictionary<VehicleResource, float> ResourceRequests;
	protected float Satisfaction;

	protected virtual void Awake()
	{
		Body = GetComponentInParent<Rigidbody2D>();

		ResourceRequests = new Dictionary<VehicleResource, float>();
	}

	protected virtual void OnEnable()
	{
		ExecuteEvents.ExecuteHierarchy<IPropulsionBlockRegistry>(
			gameObject, null, (handler, _) => handler.RegisterBlock(this)
		);
		ExecuteEvents.ExecuteHierarchy<IResourceConsumerBlockRegistry>(
			gameObject, null, (handler, _) => handler.RegisterBlock(this)
		);
	}

	protected virtual void Start()
	{
		var photonView = GetComponentInParent<PhotonView>();
		IsMine = photonView == null || photonView.IsMine;
	}

	protected virtual void OnDisable()
	{
		ExecuteEvents.ExecuteHierarchy<IPropulsionBlockRegistry>(
			gameObject, null, (handler, _) => handler.UnregisterBlock(this)
		);
		ExecuteEvents.ExecuteHierarchy<IResourceConsumerBlockRegistry>(
			gameObject, null, (handler, _) => handler.UnregisterBlock(this)
		);
	}

	public abstract void SetPropulsionCommands(float forwardBackCommand, float strafeCommand, float rotateCommand);

	public abstract IDictionary<VehicleResource, float> GetResourceConsumptionRateRequest();

	public void SatisfyResourceRequestAtLevel(float level)
	{
		Satisfaction = level;
	}

	protected void CalculateResponse(
		Vector3 localUp, out float forwardBackResponse, out float strafeResponse, out float rotateResponse
	)
	{
		localUp.Normalize();
		Vector3 localPosition = Body.transform.InverseTransformPoint(transform.position) - (Vector3) Body.centerOfMass;

		forwardBackResponse = localUp.y;
		strafeResponse = localUp.x;
		rotateResponse = localUp.x * localPosition.y - localUp.y * localPosition.x;

		rotateResponse = Mathf.Abs(rotateResponse) > 1e-5 ? Mathf.Sign(rotateResponse) : 0f;
	}

	public abstract float GetMaxPropulsionForce(CardinalDirection localDirection);
}
}