﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Syy1125.OberthEffect.Blocks.Config;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Spec.Block.Propulsion;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Propulsion
{
public class OmniThruster : AbstractThrusterBase, ITooltipProvider
{
	public const string CLASS_KEY = "OmniThruster";

	private Transform _upParticleRoot;
	private ParticleSystemWrapper[] _upParticles;
	private Transform _downParticleRoot;
	private ParticleSystemWrapper[] _downParticles;
	private Transform _leftParticleRoot;
	private ParticleSystemWrapper[] _leftParticles;
	private Transform _rightParticleRoot;
	private ParticleSystemWrapper[] _rightParticles;

	private Vector3 _localRight;
	private Vector3 _localUp;
	private Vector2 _forwardBackResponse;
	private Vector2 _strafeResponse;
	private Vector2 _rotateResponse;
	private Vector2 _response;

	public void LoadSpec(OmniThrusterSpec spec)
	{
		MaxForce = spec.MaxForce;
		MaxResourceUse = spec.MaxResourceUse;
		ActivationCondition = ControlConditionHelper.CreateControlCondition(spec.ActivationCondition);

		if (spec.Particles != null)
		{
			int particleCount = spec.Particles.Length;

			_upParticleRoot = CreateParticleParent("UpParticles", 0f);
			_upParticles = new ParticleSystemWrapper[particleCount];
			_downParticleRoot = CreateParticleParent("DownParticles", 180f);
			_downParticles = new ParticleSystemWrapper[particleCount];
			_leftParticleRoot = CreateParticleParent("LeftParticles", 90f);
			_leftParticles = new ParticleSystemWrapper[particleCount];
			_rightParticleRoot = CreateParticleParent("RightParticles", -90f);
			_rightParticles = new ParticleSystemWrapper[particleCount];

			for (int i = 0; i < particleCount; i++)
			{
				ParticleSystemSpec particleSpec = spec.Particles[i];

				_upParticles[i] = RendererHelper.CreateParticleSystem(_upParticleRoot, particleSpec);
				_downParticles[i] = RendererHelper.CreateParticleSystem(_downParticleRoot, particleSpec);
				_leftParticles[i] = RendererHelper.CreateParticleSystem(_leftParticleRoot, particleSpec);
				_rightParticles[i] = RendererHelper.CreateParticleSystem(_rightParticleRoot, particleSpec);
			}
		}

		GetComponentInParent<IControlConditionProvider>()?
			.MarkControlGroupsActive(ActivationCondition.GetControlGroups());
	}

	private Transform CreateParticleParent(string objectName, float rotation)
	{
		var objectTransform = new GameObject(objectName).transform;
		objectTransform.SetParent(transform);
		objectTransform.localPosition = Vector3.zero;
		objectTransform.localRotation = Quaternion.AngleAxis(rotation, Vector3.forward);

		return objectTransform;
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
		float ratio = (Mathf.Abs(_response.x) + Mathf.Abs(_response.y)) / 2f;

		ResourceRequests.Clear();

		foreach (KeyValuePair<string, float> entry in MaxResourceUse)
		{
			ResourceRequests.Add(entry.Key, entry.Value * ratio);
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
				Body.AddForceAtPosition(transform.TransformVector(overallResponse) * MaxForce, transform.position);
			}

			if (overallResponse.x > 0)
			{
				SetParticlesStrength(_leftParticles, 0f);
				SetParticlesStrength(_rightParticles, overallResponse.x);
			}
			else
			{
				SetParticlesStrength(_leftParticles, -overallResponse.x);
				SetParticlesStrength(_rightParticles, 0f);
			}

			if (overallResponse.y > 0)
			{
				SetParticlesStrength(_upParticles, overallResponse.y);
				SetParticlesStrength(_downParticles, 0f);
			}
			else
			{
				SetParticlesStrength(_upParticles, 0f);
				SetParticlesStrength(_downParticles, -overallResponse.y);
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
		return MaxForce;
	}

	public string GetTooltip()
	{
		StringBuilder builder = new StringBuilder();
		builder.AppendLine("Maneuvering thruster")
			.AppendLine("  Omni-directional")
			.Append($"  Max thrust per direction {PhysicsUnitUtils.FormatForce(MaxForce)}");

		if (MaxResourceUse != null && MaxResourceUse.Count > 0)
		{
			builder.AppendLine()
				.Append("  Max resource usage per second ")
				.Append(string.Join(", ", VehicleResourceDatabase.Instance.FormatResourceDict(MaxResourceUse)));
		}

		return builder.ToString();
	}
}
}