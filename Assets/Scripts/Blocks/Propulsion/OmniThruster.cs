using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Syy1125.OberthEffect.Blocks.Config;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Spec.Block.Propulsion;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Propulsion
{
public class OmniThruster : AbstractPropulsionBase, ITooltipProvider, IConfigComponent
{
	public const string CONFIG_KEY = "OmniThruster";

	[NonSerialized]
	public bool RespondToTranslation;
	[NonSerialized]
	public bool RespondToRotation;

	private Transform _upParticleRoot;
	private ParticleSystem[] _upParticles;
	private Transform _downParticleRoot;
	private ParticleSystem[] _downParticles;
	private Transform _leftParticleRoot;
	private ParticleSystem[] _leftParticles;
	private Transform _rightParticleRoot;
	private ParticleSystem[] _rightParticles;
	private float[] _maxParticleSpeeds;

	private Vector2 _forwardBackResponse;
	private Vector2 _strafeResponse;
	private Vector2 _rotateResponse;
	private Vector2 _response;

	public void LoadSpec(OmniThrusterSpec spec)
	{
		MaxForce = spec.MaxForce;
		MaxResourceUse = spec.MaxResourceUse;
		ActivationCondition = spec.ActivationCondition;

		if (spec.Particles != null)
		{
			int particleCount = spec.Particles.Length;

			_upParticleRoot = CreateParticleParent("UpParticles", 0f);
			_upParticles = new ParticleSystem[particleCount];
			_downParticleRoot = CreateParticleParent("DownParticles", 180f);
			_downParticles = new ParticleSystem[particleCount];
			_leftParticleRoot = CreateParticleParent("LeftParticles", 90f);
			_leftParticles = new ParticleSystem[particleCount];
			_rightParticleRoot = CreateParticleParent("RightParticles", -90f);
			_rightParticles = new ParticleSystem[particleCount];

			_maxParticleSpeeds = new float[particleCount];

			for (int i = 0; i < particleCount; i++)
			{
				ParticleSystemSpec particleSpec = spec.Particles[i];

				_upParticles[i] = CreateParticleSystem(_upParticleRoot, particleSpec);
				_downParticles[i] = CreateParticleSystem(_downParticleRoot, particleSpec);
				_leftParticles[i] = CreateParticleSystem(_leftParticleRoot, particleSpec);
				_rightParticles[i] = CreateParticleSystem(_rightParticleRoot, particleSpec);

				_maxParticleSpeeds[i] = particleSpec.MaxSpeed;
			}
		}
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
			Vector3 localRight = Body.transform.InverseTransformDirection(transform.right);
			Vector3 localUp = Body.transform.InverseTransformDirection(transform.up);

			CalculateResponse(localRight, out _forwardBackResponse.x, out _strafeResponse.x, out _rotateResponse.x);
			CalculateResponse(localUp, out _forwardBackResponse.y, out _strafeResponse.y, out _rotateResponse.y);

			StartParticleSystems(_upParticles);
			StartParticleSystems(_downParticles);
			StartParticleSystems(_leftParticles);
			StartParticleSystems(_rightParticles);
		}

		StartCoroutine(LateFixedUpdate());
	}

	private static void StartParticleSystems(ParticleSystem[] particleSystems)
	{
		if (particleSystems == null) return;

		foreach (ParticleSystem particle in particleSystems)
		{
			particle.Play();
		}
	}

	#region Config

	public JObject ExportConfig()
	{
		return new JObject
		{
			{ "RespondToTranslation", new JValue(RespondToTranslation) },
			{ "RespondToRotation", new JValue(RespondToRotation) }
		};
	}

	public void InitDefaultConfig()
	{
		RespondToTranslation = true;
		RespondToRotation = true;
	}

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

	#endregion

	public override void SetPropulsionCommands(Vector2 translateCommand, float rotateCommand)
	{
		if (!PropulsionActive)
		{
			_response = Vector2.zero;
			return;
		}

		Vector2 rawResponse = Vector2.zero;

		if (RespondToTranslation)
		{
			rawResponse += _forwardBackResponse * translateCommand.y + _strafeResponse * translateCommand.x;
		}

		if (RespondToRotation)
		{
			rawResponse += _rotateResponse * rotateCommand;
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

	private void SetParticlesStrength(ParticleSystem[] particles, float strength)
	{
		if (particles == null) return;

		for (int i = 0; i < particles.Length; i++)
		{
			var main = particles[i].main;
			main.startSpeedMultiplier = strength * _maxParticleSpeeds[i];
			var startColor = main.startColor.color;
			startColor.a = strength;
			main.startColor = startColor;
		}
	}

	public override float GetMaxPropulsionForce(CardinalDirection localDirection)
	{
		return MaxForce;
	}

	public string GetTooltip()
	{
		return string.Join(
			"\n",
			"Maneuvering thruster",
			"  Omni-directional",
			$"  Max thrust per direction {MaxForce * PhysicsConstants.KN_PER_UNIT_FORCE:#,0.#}kN",
			"  Max resource usage per second "
			+ string.Join(
				", ",
				VehicleResourceDatabase.Instance.FormatResourceDict(MaxResourceUse)
			)
		);
	}
}
}