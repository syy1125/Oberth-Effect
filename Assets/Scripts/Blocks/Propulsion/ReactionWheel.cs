using System.Collections;
using System.Collections.Generic;
using System.Text;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.ControlCondition;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Spec.Block.Propulsion;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Propulsion
{
public class ReactionWheel : MonoBehaviour, IPropulsionBlock, IResourceConsumer, IControlConditionReceiver,
	ITooltipProvider
{
	private float _maxTorque;
	private Dictionary<string, float> _maxResourceUse;
	private IControlCondition _activationCondition;

	private Rigidbody2D _body;
	private bool _isMine;

	private bool _active;
	private float _response;
	private Dictionary<string, float> _resourceRequests;
	private float _satisfaction;

	private void Awake()
	{
		_body = GetComponentInParent<Rigidbody2D>();
		_resourceRequests = new Dictionary<string, float>();
	}

	private void OnEnable()
	{
		GetComponentInParent<IPropulsionBlockRegistry>()?.RegisterBlock(this);
		GetComponentInParent<IResourceConsumerRegistry>()?.RegisterBlock(this);
		GetComponentInParent<IControlConditionProvider>()?.RegisterBlock(this);
	}

	public void LoadSpec(ReactionWheelSpec spec)
	{
		_maxTorque = spec.MaxTorque;
		_maxResourceUse = spec.MaxResourceUse;
		_activationCondition = ControlConditionHelper.CreateControlCondition(spec.ActivationCondition);

		GetComponentInParent<IControlConditionProvider>()
			?.MarkControlGroupsActive(_activationCondition.GetControlGroups());
	}

	private void Start()
	{
		var photonView = GetComponentInParent<PhotonView>();
		_isMine = photonView == null || photonView.IsMine;

		var provider = GetComponentInParent<IControlConditionProvider>();
		if (provider != null)
		{
			_active = provider.IsConditionTrue(_activationCondition);
		}

		StartCoroutine(LateFixedUpdate());
	}

	private void OnDisable()
	{
		GetComponentInParent<IPropulsionBlockRegistry>()?.UnregisterBlock(this);
		GetComponentInParent<IResourceConsumerRegistry>()?.UnregisterBlock(this);
		GetComponentInParent<IControlConditionProvider>()?.UnregisterBlock(this);
	}

	public void OnControlGroupsChanged(IControlConditionProvider provider)
	{
		_active = provider.IsConditionTrue(_activationCondition);
	}

	public void SetPropulsionCommands(InputCommand horizontal, InputCommand vertical, InputCommand rotate)
	{
		if (!_active)
		{
			_response = 0f;
			return;
		}

		_response = -rotate.NetValue;
	}

	public IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest()
	{
		_resourceRequests.Clear();
		float powerFraction = Mathf.Abs(_response);

		foreach (KeyValuePair<string, float> entry in _maxResourceUse)
		{
			_resourceRequests.Add(entry.Key, entry.Value * powerFraction);
		}

		return _resourceRequests;
	}

	public float SatisfyResourceRequestAtLevel(float level)
	{
		_satisfaction = level;
		return _satisfaction;
	}

	private IEnumerator LateFixedUpdate()
	{
		yield return new WaitForFixedUpdate();

		while (isActiveAndEnabled)
		{
			float trueResponse = _response * _satisfaction;

			if (_body != null && _isMine)
			{
				_body.AddTorque(trueResponse * _maxTorque);
			}

			yield return new WaitForFixedUpdate();
		}
	}

	public bool RespondToTranslation => false;
	public bool RespondToRotation => true;

	public Vector2 GetPropulsionForceOrigin()
	{
		return Vector2.zero;
	}

	public float GetMaxPropulsionForce(CardinalDirection localDirection)
	{
		return 0f;
	}

	public float GetMaxFreeTorqueCcw()
	{
		return _maxTorque;
	}

	public float GetMaxFreeTorqueCw()
	{
		return _maxTorque;
	}

	public IReadOnlyDictionary<string, float> GetMaxResourceUseRate()
	{
		return _maxResourceUse;
	}

	public string GetTooltip()
	{
		StringBuilder builder = new StringBuilder();

		builder.AppendLine("Reaction wheel")
			.Append($"  Max torque {PhysicsUnitUtils.FormatTorque(_maxTorque)}");

		if (_maxResourceUse != null && _maxResourceUse.Count > 0)
		{
			builder.AppendLine()
				.Append("  Max resource usage per second ")
				.Append(string.Join(", ", VehicleResourceDatabase.Instance.FormatResourceDict(_maxResourceUse)));
		}

		return builder.ToString();
	}
}
}