using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks.Config;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.ControlCondition;
using Syy1125.OberthEffect.Common.Enums;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Propulsion
{
public abstract class AbstractThrusterBase :
	MonoBehaviour,
	IPropulsionBlock,
	IResourceConsumer,
	IControlConditionReceiver,
	IConfigComponent
{
	protected float MaxForce;
	protected Dictionary<string, float> MaxResourceUse;
	protected IControlCondition ActivationCondition;

	protected Rigidbody2D Body;
	protected bool IsMine;

	protected bool PropulsionActive;
	protected Dictionary<string, float> ResourceRequests;
	protected float Satisfaction;

	public bool RespondToTranslation { get; protected set; }
	public bool RespondToRotation { get; protected set; }

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

	protected bool IsSimulation()
	{
		return Body != null;
	}

	public void OnControlGroupsChanged(IControlConditionProvider provider)
	{
		PropulsionActive = provider.IsConditionTrue(ActivationCondition);
	}

	public void SetPropulsionCommands(InputCommand horizontal, InputCommand vertical, InputCommand rotate)
	{
		SetPropulsionCommands(horizontal.NetValue, vertical.NetValue, rotate.NetValue);
	}

	protected abstract void SetPropulsionCommands(float horizontal, float vertical, float rotate);

	public abstract IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest();

	public virtual float SatisfyResourceRequestAtLevel(float level)
	{
		Satisfaction = level;
		return Satisfaction;
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
		Vector3 localPosition =
			Body.transform.InverseTransformPoint(transform.TransformPoint(GetPropulsionForceOrigin()))
			- (Vector3) Body.centerOfMass;

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

	public float GetMaxFreeTorqueCcw()
	{
		return 0f;
	}

	public float GetMaxFreeTorqueCw()
	{
		return 0f;
	}

	public virtual IReadOnlyDictionary<string, float> GetMaxResourceUseRate()
	{
		return MaxResourceUse;
	}

	public JObject ExportConfig()
	{
		return new JObject
		{
			{ "RespondToTranslation", new JValue(RespondToTranslation) },
			{ "RespondToRotation", new JValue(RespondToRotation) }
		};
	}

	public abstract void InitDefaultConfig();

	public void ImportConfig(JObject config)
	{
		if (config.ContainsKey("RespondToTranslation"))
		{
			RespondToTranslation = config["RespondToTranslation"].Value<bool>();
		}

		if (config.ContainsKey("RespondToRotation"))
		{
			RespondToRotation = config["RespondToRotation"].Value<bool>();
		}
	}

	public List<ConfigItemBase> GetConfigItems()
	{
		return new List<ConfigItemBase>
		{
			new ToggleConfigItem
			{
				Key = "RespondToTranslation",
				Label = "Respond to translation"
			},
			new ToggleConfigItem
			{
				Key = "RespondToRotation",
				Label = "Respond to rotation"
			}
		};
	}
}
}