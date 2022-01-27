using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Spec.Block.Propulsion;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Propulsion
{
public class DirectionalThruster : AbstractThrusterBase, ITooltipProvider
{
	public const string CLASS_KEY = "DirectionalThruster";

	private ParticleSystemWrapper[] _upParticles;
	private ParticleSystemWrapper[] _downParticles;
	private ParticleSystemWrapper[] _leftParticles;
	private ParticleSystemWrapper[] _rightParticles;

	private float _upMaxForce;
	private Dictionary<string, float> _upResourceUse;
	private float _downMaxForce;
	private Dictionary<string, float> _downResourceUse;
	private float _leftMaxForce;
	private Dictionary<string, float> _leftResourceUse;
	private float _rightMaxForce;
	private Dictionary<string, float> _rightResourceUse;

	private Vector3 _localRight;
	private Vector3 _localUp;
	private Vector2 _forwardBackResponse;
	private Vector2 _strafeResponse;
	private Vector2 _rotateResponse;
	private Vector2 _response;

	public void LoadSpec(DirectionalThrusterSpec spec)
	{
		ActivationCondition = ControlConditionHelper.CreateControlCondition(spec.ActivationCondition);

		if (spec.Up != null)
		{
			LoadModuleSpec(spec.Up, out _upMaxForce, out _upResourceUse, out _upParticles);
		}

		if (spec.Down != null)
		{
			LoadModuleSpec(spec.Down, out _downMaxForce, out _downResourceUse, out _downParticles);
		}

		if (spec.Left != null)
		{
			LoadModuleSpec(spec.Left, out _leftMaxForce, out _leftResourceUse, out _leftParticles);
		}

		if (spec.Right != null)
		{
			LoadModuleSpec(spec.Right, out _rightMaxForce, out _rightResourceUse, out _rightParticles);
		}

		ComputeMaxResourceUse();

		GetComponentInParent<IControlConditionProvider>()
			?.MarkControlGroupsActive(ActivationCondition.GetControlGroups());
	}

	private void LoadModuleSpec(
		DirectionalThrusterModuleSpec spec,
		out float maxForce, out Dictionary<string, float> maxResourceUse, out ParticleSystemWrapper[] particles
	)
	{
		maxForce = spec.MaxForce;
		maxResourceUse = spec.MaxResourceUse;

		if (spec.Particles != null)
		{
			particles = new ParticleSystemWrapper[spec.Particles.Length];

			for (int i = 0; i < spec.Particles.Length; i++)
			{
				particles[i] = RendererHelper.CreateParticleSystem(transform, spec.Particles[i]);
			}
		}
		else
		{
			particles = null;
		}
	}

	private void ComputeMaxResourceUse()
	{
		var horizontalResourceUse = new Dictionary<string, float>();

		if (_leftResourceUse != null)
		{
			DictionaryUtils.MergeDictionary(_leftResourceUse, horizontalResourceUse, FloatMax);
		}

		if (_rightResourceUse != null)
		{
			DictionaryUtils.MergeDictionary(_rightResourceUse, horizontalResourceUse, FloatMax);
		}

		var verticalResourceUse = new Dictionary<string, float>();

		if (_upResourceUse != null)
		{
			DictionaryUtils.MergeDictionary(_upResourceUse, verticalResourceUse, FloatMax);
		}

		if (_downResourceUse != null)
		{
			DictionaryUtils.MergeDictionary(_downResourceUse, verticalResourceUse, FloatMax);
		}

		MaxResourceUse = new Dictionary<string, float>();
		DictionaryUtils.SumDictionaries(new[] { horizontalResourceUse, verticalResourceUse }, MaxResourceUse);
	}

	private static float FloatMax(float left, float right)
	{
		return Mathf.Max(left, right);
	}

	protected override void Start()
	{
		base.Start();

		if (Body != null)
		{
			_localRight = Body.transform.InverseTransformDirection(transform.right);
			_localUp = Body.transform.InverseTransformDirection(transform.up);

			StartParticleSystems(_upParticles);
			StartParticleSystems(_downParticles);
			StartParticleSystems(_leftParticles);
			StartParticleSystems(_rightParticles);
		}

		StartCoroutine(LateFixedUpdate());
	}

	private static void StartParticleSystems(ParticleSystemWrapper[] particleSystems)
	{
		if (particleSystems == null) return;

		foreach (ParticleSystemWrapper particle in particleSystems)
		{
			particle.Play();
		}
	}

	public override void InitDefaultConfig()
	{
		RespondToTranslation = true;
		RespondToRotation = true;
	}

	protected override void SetPropulsionCommands(float horizontal, float vertical, float rotate)
	{
		if (!PropulsionActive)
		{
			_response = Vector2.zero;
			return;
		}

		CalculateResponse(_localRight, out _forwardBackResponse.x, out _strafeResponse.x, out _rotateResponse.x);
		CalculateResponse(_localUp, out _forwardBackResponse.y, out _strafeResponse.y, out _rotateResponse.y);

		Vector2 rawResponse = Vector2.zero;

		if (RespondToTranslation)
		{
			rawResponse += _forwardBackResponse * vertical + _strafeResponse * horizontal;
		}

		if (RespondToRotation)
		{
			rawResponse += _rotateResponse * rotate;
		}

		_response = new Vector2(
			Mathf.Clamp(rawResponse.x, -1f, 1f),
			Mathf.Clamp(rawResponse.y, -1f, 1f)
		);
	}

	public override IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest()
	{
		ResourceRequests.Clear();

		if (_response.x > Mathf.Epsilon && _rightMaxForce > Mathf.Epsilon && _rightResourceUse != null)
		{
			DictionaryUtils.AddScaledDictionary(_rightResourceUse, _response.x, ResourceRequests);
		}
		else if (_response.x < -Mathf.Epsilon && _leftMaxForce > Mathf.Epsilon && _leftResourceUse != null)
		{
			DictionaryUtils.AddScaledDictionary(_leftResourceUse, -_response.x, ResourceRequests);
		}

		if (_response.y > Mathf.Epsilon && _upMaxForce > Mathf.Epsilon && _upResourceUse != null)
		{
			DictionaryUtils.AddScaledDictionary(_upResourceUse, _response.y, ResourceRequests);
		}
		else if (_response.y < -Mathf.Epsilon && _downMaxForce > Mathf.Epsilon && _downResourceUse != null)
		{
			DictionaryUtils.AddScaledDictionary(_downResourceUse, -_response.y, ResourceRequests);
		}

		return ResourceRequests;
	}

	private IEnumerator LateFixedUpdate()
	{
		yield return new WaitForFixedUpdate();

		while (enabled)
		{
			Vector2 overallResponse = _response * Satisfaction;

			if (Body != null && IsMine)
			{
				Vector2 right = transform.right;
				Vector2 up = transform.up;
				Vector2 position = transform.position;

				if (overallResponse.x > Mathf.Epsilon && _rightMaxForce > Mathf.Epsilon)
				{
					Body.AddForceAtPosition(right * (overallResponse.x * _rightMaxForce), position);
					SetParticlesStrength(_leftParticles, 0f);
					SetParticlesStrength(_rightParticles, overallResponse.x);
				}
				else if (overallResponse.x < -Mathf.Epsilon && _leftMaxForce > Mathf.Epsilon)
				{
					Body.AddForceAtPosition(right * (overallResponse.x * _leftMaxForce), position);
					SetParticlesStrength(_leftParticles, -overallResponse.x);
					SetParticlesStrength(_rightParticles, 0f);
				}
				else
				{
					SetParticlesStrength(_leftParticles, 0f);
					SetParticlesStrength(_rightParticles, 0f);
				}

				if (overallResponse.y > Mathf.Epsilon && _upMaxForce > Mathf.Epsilon)
				{
					Body.AddForceAtPosition(up * (overallResponse.y * _upMaxForce), position);
					SetParticlesStrength(_upParticles, overallResponse.y);
					SetParticlesStrength(_downParticles, 0f);
				}
				else if (overallResponse.y < -Mathf.Epsilon && _downMaxForce > Mathf.Epsilon)
				{
					Body.AddForceAtPosition(up * (overallResponse.y * _downMaxForce), position);
					SetParticlesStrength(_upParticles, 0f);
					SetParticlesStrength(_downParticles, -overallResponse.y);
				}
				else
				{
					SetParticlesStrength(_upParticles, 0f);
					SetParticlesStrength(_downParticles, 0f);
				}
			}

			yield return new WaitForFixedUpdate();
		}
	}

	private static void SetParticlesStrength(ParticleSystemWrapper[] particles, float thrustScale)
	{
		if (particles != null) ParticleSystemWrapper.BatchScaleThrustParticles(particles, thrustScale);
	}

	public override float GetMaxPropulsionForce(CardinalDirection localDirection)
	{
		switch (localDirection)
		{
			case CardinalDirection.Up:
				return _upMaxForce;
			case CardinalDirection.Right:
				return _rightMaxForce;
			case CardinalDirection.Down:
				return _downMaxForce;
			case CardinalDirection.Left:
				return _leftMaxForce;
			default:
				throw new ArgumentOutOfRangeException(nameof(localDirection), localDirection, null);
		}
	}

	public string GetTooltip()
	{
		StringBuilder builder = new StringBuilder();
		builder.AppendLine("Maneuvering thruster");

		AppendDirectionTooltip(builder, "upward", _upMaxForce, _upResourceUse);
		AppendDirectionTooltip(builder, "downward", _downMaxForce, _downResourceUse);
		AppendDirectionTooltip(builder, "left", _leftMaxForce, _leftResourceUse);
		AppendDirectionTooltip(builder, "right", _rightMaxForce, _rightResourceUse);

		return builder.ToString();
	}

	private static void AppendDirectionTooltip(
		StringBuilder builder, string direction, float maxForce, IReadOnlyDictionary<string, float> maxResourceUse
	)
	{
		if (maxForce > Mathf.Epsilon)
		{
			builder.Append($"  Max thrust {direction} {PhysicsUnitUtils.FormatForce(maxForce)}");

			if (maxResourceUse != null && maxResourceUse.Count > 0)
			{
				builder.Append(" using up to ")
					.Append(string.Join(", ", VehicleResourceDatabase.Instance.FormatResourceDict(maxResourceUse)))
					.Append(" per second");
			}

			builder.AppendLine();
		}
	}
}
}