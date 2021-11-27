using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Propulsion
{
public abstract class AbstractPropulsionBase :
	MonoBehaviour,
	IPropulsionBlock,
	IResourceConsumer,
	IControlConditionReceiver
{
	protected float MaxForce;
	protected Dictionary<string, float> MaxResourceUse;
	protected ControlConditionSpec ActivationCondition;

	protected Rigidbody2D Body;
	protected bool IsMine;

	protected bool PropulsionActive;
	protected Dictionary<string, float> ResourceRequests;
	protected float Satisfaction;

	protected virtual void Awake()
	{
		Body = GetComponentInParent<Rigidbody2D>();

		ResourceRequests = new Dictionary<string, float>();
	}

	protected virtual void OnEnable()
	{
		GetComponentInParent<IPropulsionBlockRegistry>()?.RegisterBlock(this);
		GetComponentInParent<IResourceConsumerRegistry>()?.RegisterBlock(this);
		GetComponentInParent<IControlConditionProvider>()?.RegisterBlock(this);
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

		var provider = GetComponentInParent<IControlConditionProvider>();
		if (provider != null)
		{
			PropulsionActive = provider.IsConditionTrue(ActivationCondition);
		}
	}

	protected virtual void OnDisable()
	{
		GetComponentInParent<IPropulsionBlockRegistry>()?.UnregisterBlock(this);
		GetComponentInParent<IResourceConsumerRegistry>()?.UnregisterBlock(this);
		GetComponentInParent<IControlConditionProvider>()?.UnregisterBlock(this);
	}

	public void OnControlGroupsChanged(IControlConditionProvider provider)
	{
		PropulsionActive = provider.IsConditionTrue(ActivationCondition);
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
		if (Body == null)
		{
			forwardBackResponse = 0f;
			strafeResponse = 0f;
			rotateResponse = 0f;
			return;
		}

		localUp.Normalize();
		Vector3 localPosition = Body.transform.InverseTransformPoint(transform.position) - (Vector3) Body.centerOfMass;

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