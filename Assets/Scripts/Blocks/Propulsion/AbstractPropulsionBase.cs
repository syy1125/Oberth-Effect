using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Propulsion
{
public abstract class AbstractPropulsionBase : MonoBehaviour, IPropulsionBlock, IResourceConsumerBlock
{
	protected float MaxForce;
	protected Dictionary<string, float> MaxResourceUse;
	protected bool IsFuelPropulsion;

	protected Rigidbody2D Body;
	protected CenterOfMassContext MassContext;
	protected bool IsMine;

	protected bool FuelPropulsionActive;
	protected Dictionary<string, float> ResourceRequests;
	protected float Satisfaction;

	protected virtual void Awake()
	{
		Body = GetComponentInParent<Rigidbody2D>();
		MassContext = GetComponentInParent<CenterOfMassContext>();

		ResourceRequests = new Dictionary<string, float>();
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

	protected static ParticleSystem CreateParticleSystem(Transform parent, ParticleSystemSpec spec)
	{
		GameObject particleHolder = new GameObject("ParticleSystem");

		var holderTransform = particleHolder.transform;
		holderTransform.SetParent(parent);
		holderTransform.localPosition = new Vector3(spec.Offset.x, spec.Offset.y, 1f);
		holderTransform.localRotation = Quaternion.LookRotation(spec.Direction);

		var particles = particleHolder.AddComponent<ParticleSystem>();
		particles.LoadSpec(spec);
		particles.Stop();
		return particles;
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

	public void SetFuelPropulsionActive(bool fuelActive)
	{
		FuelPropulsionActive = fuelActive;
	}

	public abstract void SetPropulsionCommands(Vector2 translateCommand, float rotateCommand);

	public abstract IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest();

	public void SatisfyResourceRequestAtLevel(float level)
	{
		Satisfaction = level;
	}

	protected void CalculateResponse(
		Vector3 localUp, out float forwardBackResponse, out float strafeResponse, out float rotateResponse
	)
	{
		localUp.Normalize();
		Vector3 localPosition = MassContext.transform.InverseTransformPoint(transform.position)
		                        - (Vector3) MassContext.GetCenterOfMass();

		forwardBackResponse = localUp.y;
		strafeResponse = localUp.x;
		rotateResponse = localUp.x * localPosition.y - localUp.y * localPosition.x;

		rotateResponse = Mathf.Abs(rotateResponse) > 1e-5 ? Mathf.Sign(rotateResponse) : 0f;
	}

	public virtual Vector2 GetPropulsionForceOrigin()
	{
		return Vector2.zero;
	}

	public abstract float GetMaxPropulsionForce(CardinalDirection localDirection);

	public virtual IReadOnlyDictionary<string, float> GetMaxResourceUseRate()
	{
		return MaxResourceUse;
	}
}
}