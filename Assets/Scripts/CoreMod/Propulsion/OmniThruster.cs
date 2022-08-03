using System.Collections.Generic;
using System.Text;
using Syy1125.OberthEffect.CoreMod.Propulsion;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Foundation.Utils;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;
using Syy1125.OberthEffect.Spec.Unity;
using Syy1125.OberthEffect.Spec.Validation;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Propulsion
{
[CreateSchemaFile("OmniThrusterSpecSchema")]
public class OmniThrusterSpec : ICustomValidation
{
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxForce;
	public Dictionary<string, float> MaxResourceUse;
	public ControlConditionSpec ActivationCondition;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public SoundCurveSpec ThrustSound;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public ParticleSystemSpec[] Particles;

	public void Validate(List<string> path, List<string> errors)
	{
		ValidationHelper.ValidateFields(path, this, errors);
		path.Add(nameof(MaxResourceUse));
		ValidationHelper.ValidateResourceDictionary(path, MaxResourceUse, errors);
		path.RemoveAt(path.Count - 1);
	}
}

public class OmniThruster : AbstractThrusterBase, IBlockComponent<OmniThrusterSpec>, ITooltipComponent
{
	public const string CLASS_KEY = "OmniThruster";

	private string _thrustSoundId;
	private AudioSource _thrustSoundSource;
	private float _minVolume;
	private float _maxVolume;

	private ParticleSystemWrapper[] _upParticles;
	private ParticleSystemWrapper[] _downParticles;
	private ParticleSystemWrapper[] _leftParticles;
	private ParticleSystemWrapper[] _rightParticles;

	private Vector3 _localRight;
	private Vector3 _localUp;
	private Vector2 _forwardBackResponse;
	private Vector2 _strafeResponse;
	private Vector2 _rotateResponse;
	private Vector2 _response;

	public void LoadSpec(OmniThrusterSpec spec, in BlockContext context)
	{
		MaxForce = spec.MaxForce;
		MaxResourceUse = spec.MaxResourceUse;
		ActivationCondition = ControlConditionHelper.CreateControlCondition(spec.ActivationCondition);

		if (spec.ThrustSound != null)
		{
			_thrustSoundId = spec.ThrustSound.SoundId;
			_thrustSoundSource = SoundDatabase.Instance.CreateBlockAudioSource(gameObject, !context.IsMainVehicle);
			_minVolume = spec.ThrustSound.MinVolume;
			_maxVolume = spec.ThrustSound.MaxVolume;

			_thrustSoundSource.clip = SoundDatabase.Instance.GetAudioClip(spec.ThrustSound.SoundId);
			_thrustSoundSource.volume = _minVolume;
			_thrustSoundSource.loop = true;
		}

		if (spec.Particles != null)
		{
			int particleCount = spec.Particles.Length;

			_upParticles = RendererHelper.CreateParticleSystems(
				CreateParticleParent("UpParticles", 0f), spec.Particles
			);
			_downParticles = RendererHelper.CreateParticleSystems(
				CreateParticleParent("DownParticles", 180f), spec.Particles
			);
			_leftParticles = RendererHelper.CreateParticleSystems(
				CreateParticleParent("LeftParticles", 90f), spec.Particles
			);
			_rightParticles = RendererHelper.CreateParticleSystems(
				CreateParticleParent("RightParticles", -90f), spec.Particles
			);
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

		if (IsSimulation())
		{
			_localRight = Body.transform.InverseTransformDirection(transform.right);
			_localUp = Body.transform.InverseTransformDirection(transform.up);

			if (_thrustSoundSource != null)
			{
				_thrustSoundSource.Play();
			}

			StartParticleSystems(_upParticles);
			StartParticleSystems(_downParticles);
			StartParticleSystems(_leftParticles);
			StartParticleSystems(_rightParticles);
		}
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

	private void FixedUpdate()
	{
		Vector2 overallResponse = _response * Satisfaction;

		if (IsSimulation())
		{
			if (IsMine)
			{
				Body.AddForceAtPosition(transform.TransformVector(overallResponse) * MaxForce, transform.position);
			}

			if (_thrustSoundSource != null)
			{
				float volume = Mathf.Lerp(
					_minVolume, _maxVolume,
					(Mathf.Abs(overallResponse.x) + Mathf.Abs(overallResponse.y)) / 2
				);
				_thrustSoundSource.volume = SoundAttenuator.AttenuatePersistentSound(_thrustSoundId, volume);
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

	public bool GetTooltip(StringBuilder builder, string indent)
	{
		builder.AppendLine($"{indent}Maneuvering thruster")
			.AppendLine($"{indent}  Omni-directional")
			.AppendLine($"{indent}  Max thrust per direction {PhysicsUnitUtils.FormatForce(MaxForce)}");

		if (MaxResourceUse != null && MaxResourceUse.Count > 0)
		{
			builder.Append($"{indent}  Max resource usage per second ")
				.Append(string.Join(", ", VehicleResourceDatabase.Instance.FormatResourceDict(MaxResourceUse)))
				.AppendLine();
		}

		return true;
	}
}
}