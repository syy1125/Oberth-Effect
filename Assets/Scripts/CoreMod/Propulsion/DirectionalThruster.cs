﻿using System;
using System.Collections.Generic;
using System.Drawing.Design;
using System.Text;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Foundation.Utils;
using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;
using Syy1125.OberthEffect.Spec.Unity;
using Syy1125.OberthEffect.Spec.Validation;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.CoreMod.Propulsion
{
[CreateSchemaFile("DirectionalThrusterModuleSpecSchema")]
public class DirectionalThrusterModuleSpec : ICustomValidation
{
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxForce;
	public Dictionary<string, float> MaxResourceUse;
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

[CreateSchemaFile("DirectionalThrusterSpecSchema")]
public class DirectionalThrusterSpec
{
	public DirectionalThrusterModuleSpec Up;
	public DirectionalThrusterModuleSpec Down;
	public DirectionalThrusterModuleSpec Left;
	public DirectionalThrusterModuleSpec Right;

	public ControlConditionSpec ActivationCondition;
}

public class DirectionalThruster : AbstractThrusterBase, IBlockComponent<DirectionalThrusterSpec>, ITooltipComponent
{
	public const string CLASS_KEY = "DirectionalThruster";

	private class Module
	{
		private DirectionalThruster _parent;

		public readonly float MaxForce;
		public readonly Dictionary<string, float> MaxResourceUse;

		private string _thrustSoundId;
		private AudioSource _thrustSoundSource;
		private float _minVolume;
		private float _maxVolume;
		private ParticleSystemWrapper[] _particles;

		public Module(DirectionalThruster parent, DirectionalThrusterModuleSpec spec, in BlockContext context)
		{
			_parent = parent;

			MaxForce = spec.MaxForce;
			MaxResourceUse = spec.MaxResourceUse;

			if (spec.ThrustSound != null)
			{
				_thrustSoundId = spec.ThrustSound.SoundId;
				_thrustSoundSource = SoundDatabase.Instance.CreateBlockAudioSource(
					parent.gameObject, !context.IsMainVehicle
				);
				_minVolume = spec.ThrustSound.MinVolume;
				_maxVolume = spec.ThrustSound.MaxVolume;

				_thrustSoundSource.clip = SoundDatabase.Instance.GetAudioClip(_thrustSoundId);
				_thrustSoundSource.volume = _minVolume;
				_thrustSoundSource.loop = true;
			}

			if (spec.Particles != null)
			{
				_particles = RendererHelper.CreateParticleSystems(_parent.transform, spec.Particles);
			}
		}

		public void StartEffects(BlockEnvironment environment)
		{
			if (_thrustSoundSource != null && environment == BlockEnvironment.Simulation)
			{
				_thrustSoundSource.Play();
			}

			if (_particles != null)
			{
				ParticleSystemWrapper.BatchPlay(_particles);

				if (environment == BlockEnvironment.Preview)
				{
					ParticleSystemWrapper.BatchScaleThrustParticles(_particles, 1);
				}
			}
		}

		public void AddResourceRequest(IDictionary<string, float> requests, float requestStrength)
		{
			if (MaxResourceUse != null && MaxResourceUse.Count > 0)
			{
				DictionaryUtils.AddScaledDictionary(MaxResourceUse, requestStrength, requests);
			}
		}

		public void PlayEffectsAtStrength(float thrustScale)
		{
			if (_thrustSoundSource != null)
			{
				float volume = Mathf.Lerp(_minVolume, _maxVolume, thrustScale);
				_thrustSoundSource.volume = _parent.SoundAttenuator.AttenuatePersistentSound(_thrustSoundId, volume);
			}

			if (_particles != null)
			{
				ParticleSystemWrapper.BatchScaleThrustParticles(_particles, thrustScale);
			}
		}
	}

	private Module _upModule;
	private Module _downModule;
	private Module _leftModule;
	private Module _rightModule;

	private Vector3 _localRight;
	private Vector3 _localUp;
	private Vector2 _forwardBackResponse;
	private Vector2 _strafeResponse;
	private Vector2 _rotateResponse;
	private Vector2 _response;

	public void LoadSpec(DirectionalThrusterSpec spec, in BlockContext context)
	{
		Environment = context.Environment;

		ActivationCondition = ControlConditionHelper.CreateControlCondition(spec.ActivationCondition);

		if (spec.Up != null)
		{
			_upModule = new(this, spec.Up, context);
		}

		if (spec.Down != null)
		{
			_downModule = new(this, spec.Down, context);
		}

		if (spec.Left != null)
		{
			_leftModule = new(this, spec.Left, context);
		}

		if (spec.Right != null)
		{
			_rightModule = new(this, spec.Right, context);
		}

		ComputeMaxResourceUse();

		GetComponentInParent<IControlConditionProvider>()
			?.MarkControlGroupsActive(ActivationCondition.GetControlGroups());
	}

	private void ComputeMaxResourceUse()
	{
		var horizontalResourceUse = new Dictionary<string, float>();

		if (_leftModule != null)
		{
			DictionaryUtils.MergeDictionary(_leftModule.MaxResourceUse, horizontalResourceUse, Mathf.Max);
		}

		if (_rightModule != null)
		{
			DictionaryUtils.MergeDictionary(_rightModule.MaxResourceUse, horizontalResourceUse, Mathf.Max);
		}

		var verticalResourceUse = new Dictionary<string, float>();

		if (_upModule != null)
		{
			DictionaryUtils.MergeDictionary(_upModule.MaxResourceUse, verticalResourceUse, Mathf.Max);
		}

		if (_downModule != null)
		{
			DictionaryUtils.MergeDictionary(_downModule.MaxResourceUse, verticalResourceUse, Mathf.Max);
		}

		MaxResourceUse = new();
		DictionaryUtils.SumDictionaries(new[] { horizontalResourceUse, verticalResourceUse }, MaxResourceUse);
	}

	protected override void Start()
	{
		base.Start();

		switch (Environment)
		{
			case BlockEnvironment.Palette:
			case BlockEnvironment.Designer:
				break;
			case BlockEnvironment.Preview:

				_upModule?.StartEffects(Environment);
				_downModule?.StartEffects(Environment);
				_leftModule?.StartEffects(Environment);
				_rightModule?.StartEffects(Environment);
				break;

			case BlockEnvironment.Simulation:
				_localRight = Body.transform.InverseTransformDirection(transform.right);
				_localUp = Body.transform.InverseTransformDirection(transform.up);

				_upModule?.StartEffects(Environment);
				_downModule?.StartEffects(Environment);
				_leftModule?.StartEffects(Environment);
				_rightModule?.StartEffects(Environment);

				break;
			default:
				throw new ArgumentOutOfRangeException();
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

		if (_response.x > Mathf.Epsilon)
		{
			_rightModule?.AddResourceRequest(ResourceRequests, _response.x);
		}
		else if (_response.x < -Mathf.Epsilon)
		{
			_leftModule?.AddResourceRequest(ResourceRequests, -_response.x);
		}

		if (_response.y > Mathf.Epsilon)
		{
			_upModule?.AddResourceRequest(ResourceRequests, _response.y);
		}
		else if (_response.y < -Mathf.Epsilon)
		{
			_downModule?.AddResourceRequest(ResourceRequests, -_response.y);
		}

		return ResourceRequests;
	}

	private void FixedUpdate()
	{
		if (Environment != BlockEnvironment.Simulation) return;

		Vector2 overallResponse = _response * Satisfaction;

		Vector2 right = transform.right;
		Vector2 up = transform.up;
		Vector2 position = transform.position;

		if (overallResponse.x > Mathf.Epsilon && _rightModule != null)
		{
			if (IsMine)
			{
				Body.AddForceAtPosition(right * (overallResponse.x * _rightModule.MaxForce), position);
			}

			_leftModule?.PlayEffectsAtStrength(0f);
			_rightModule.PlayEffectsAtStrength(overallResponse.x);
		}
		else if (overallResponse.x < -Mathf.Epsilon && _leftModule != null)
		{
			if (IsMine)
			{
				Body.AddForceAtPosition(right * (overallResponse.x * _leftModule.MaxForce), position);
			}

			_leftModule.PlayEffectsAtStrength(-overallResponse.x);
			_rightModule?.PlayEffectsAtStrength(0f);
		}
		else
		{
			_leftModule?.PlayEffectsAtStrength(0f);
			_rightModule?.PlayEffectsAtStrength(0f);
		}

		if (overallResponse.y > Mathf.Epsilon && _upModule != null)
		{
			if (IsMine)
			{
				Body.AddForceAtPosition(up * (overallResponse.y * _upModule.MaxForce), position);
			}

			_upModule.PlayEffectsAtStrength(overallResponse.y);
			_downModule?.PlayEffectsAtStrength(0f);
		}
		else if (overallResponse.y < -Mathf.Epsilon && _downModule != null)
		{
			Body.AddForceAtPosition(up * (overallResponse.y * _downModule.MaxForce), position);

			_upModule?.PlayEffectsAtStrength(0f);
			_downModule.PlayEffectsAtStrength(-overallResponse.y);
		}
		else
		{
			_upModule?.PlayEffectsAtStrength(0f);
			_downModule?.PlayEffectsAtStrength(0f);
		}
	}

	public override float GetMaxPropulsionForce(CardinalDirection localDirection)
	{
		switch (localDirection)
		{
			case CardinalDirection.Up:
				return _upModule?.MaxForce ?? 0f;
			case CardinalDirection.Right:
				return _rightModule?.MaxForce ?? 0f;
			case CardinalDirection.Down:
				return _downModule?.MaxForce ?? 0f;
			case CardinalDirection.Left:
				return _leftModule?.MaxForce ?? 0f;
			default:
				throw new ArgumentOutOfRangeException(nameof(localDirection), localDirection, null);
		}
	}

	public bool GetTooltip(StringBuilder builder, string indent)
	{
		builder.AppendLine($"{indent}Maneuvering thruster");

		AppendDirectionTooltip(builder, indent, "upward", _upModule);
		AppendDirectionTooltip(builder, indent, "downward", _downModule);
		AppendDirectionTooltip(builder, indent, "left", _leftModule);
		AppendDirectionTooltip(builder, indent, "right", _rightModule);

		return true;
	}

	private static void AppendDirectionTooltip(
		StringBuilder builder, string indent, string direction, Module thrustModule
	)
	{
		if (thrustModule != null)
		{
			builder.Append($"{indent}  Max thrust {direction} {PhysicsUnitUtils.FormatForce(thrustModule.MaxForce)}");

			if (thrustModule.MaxResourceUse != null && thrustModule.MaxResourceUse.Count > 0)
			{
				builder.Append(" using up to ")
					.Append(
						string.Join(
							", ", VehicleResourceDatabase.Instance.FormatResourceDict(thrustModule.MaxResourceUse)
						)
					)
					.Append(" per second");
			}

			builder.AppendLine();
		}
	}
}
}