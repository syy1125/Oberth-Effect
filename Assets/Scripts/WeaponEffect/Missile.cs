﻿using System;
using System.Collections;
using Photon.Pun;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Common.Physics;
using Syy1125.OberthEffect.Lib;
using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
public struct MissileConfig
{
	public Vector2 ColliderSize;
	public float Damage;
	public DamageType DamageType;
	public float ArmorPierce;
	public float ExplosionRadius;
	public float Lifetime;

	public bool HasTarget;
	public int TargetPhotonId;
	public float MaxAcceleration;
	public float MaxAngularAcceleration;
	public MissileGuidanceAlgorithm GuidanceAlgorithm;
	public float GuidanceActivationDelay;

	public bool IsPointDefenseTarget;
	public float MaxHealth;
	public float ArmorValue;
	public float HealthDamageScaling;

	public RendererSpec[] Renderers;
	public ParticleSystemSpec[] PropulsionParticles;
}

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(DamagingProjectile))]
public class Missile : MonoBehaviourPun, IPunInstantiateMagicCallback
{
	public float RotationPidResponse = 2f;
	public float RotationPidBaseDerivativeTime = 5f;

	private Rigidbody2D _body;
	private MissileConfig _config;
	private Rigidbody2D _targetBody;
	private PointDefenseTarget _pdTarget;
	private ParticleSystem[] _propulsionParticles;
	private float[] _maxParticleSpeeds;

	private float _initTime;
	private Vector2? _targetAcceleration;
	private Pid<float> _rotationPid;

	private void Awake()
	{
		_body = GetComponent<Rigidbody2D>();
	}

	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		object[] instantiationData = info.photonView.InstantiationData;
		_config = JsonUtility.FromJson<MissileConfig>(CompressionUtils.Decompress((byte[]) instantiationData[0]));

		GetComponent<DamagingProjectile>().Init(
			_config.Damage, _config.DamageType, _config.ArmorPierce, _config.ExplosionRadius,
			GetHealthDamageModifier
		);

		GetComponent<BoxCollider2D>().size = _config.ColliderSize;

		if (_config.IsPointDefenseTarget)
		{
			_pdTarget = gameObject.AddComponent<PointDefenseTarget>();
			_pdTarget.Init(_config.MaxHealth, _config.ArmorValue, _config.ColliderSize);
			// _pdTarget.OnDestroyedByDamage.AddListener(EndOfLifeDespawn);

			gameObject.AddComponent<ReferenceFrameProvider>();
			var radiusProvider = gameObject.AddComponent<ConstantCollisionRadiusProvider>();
			radiusProvider.Radius = _config.ColliderSize.magnitude / 2;
		}

		GetComponent<ConstantCollisionRadiusProvider>().Radius = _config.ColliderSize.magnitude / 2;

		RendererHelper.AttachRenderers(transform, _config.Renderers);

		if (_config.PropulsionParticles != null)
		{
			_propulsionParticles = new ParticleSystem[_config.PropulsionParticles.Length];
			_maxParticleSpeeds = new float[_config.PropulsionParticles.Length];

			for (var i = 0; i < _config.PropulsionParticles.Length; i++)
			{
				_propulsionParticles[i] =
					RendererHelper.CreateParticleSystem(transform, _config.PropulsionParticles[i]);
				_maxParticleSpeeds[i] = _config.PropulsionParticles[i].MaxSpeed;
			}
		}

		Debug.Log($"Missile launched with HasTarget={_config.HasTarget} and TargetPhotonId={_config.TargetPhotonId}");
		if (_config.HasTarget)
		{
			_targetBody = PhotonView.Find(_config.TargetPhotonId)?.GetComponent<Rigidbody2D>();
			Debug.Log($"Target body is {_targetBody}");
		}

		_initTime = Time.time;
		_targetAcceleration = null;
		_rotationPid = new RotationPid(
			new PidConfig
			{
				Response = RotationPidResponse,
				DerivativeTime = RotationPidBaseDerivativeTime / _config.MaxAngularAcceleration
			}
		);
	}

	private void Start()
	{
		StartCoroutine(LateFixedUpdate());
		Invoke(nameof(EndOfLifeDespawn), _config.Lifetime);

		if (_propulsionParticles != null)
		{
			foreach (ParticleSystem particle in _propulsionParticles)
			{
				particle.Play();
			}
		}
	}

	private void FixedUpdate()
	{
		if (_targetAcceleration != null)
		{
			float angle = Vector2.SignedAngle(_targetAcceleration.Value, transform.up);
			_rotationPid.Update(angle, Time.fixedDeltaTime);

			float rotationResponse = Mathf.Clamp(_rotationPid.Output, -1f, 1f);
			_body.angularVelocity -= rotationResponse * _config.MaxAngularAcceleration * Time.fixedDeltaTime;

			float thrustFraction = Mathf.Clamp01(Mathf.Cos(angle * Mathf.Deg2Rad));
			_body.velocity += (Vector2) transform.up
			                  * (thrustFraction * _targetAcceleration.Value.magnitude * Time.fixedDeltaTime);

			if (_propulsionParticles != null)
			{
				for (var i = 0; i < _propulsionParticles.Length; i++)
				{
					ParticleSystem.MainModule main = _propulsionParticles[i].main;
					main.startSpeedMultiplier = thrustFraction * _maxParticleSpeeds[i];
					Color startColor = main.startColor.color;
					startColor.a = thrustFraction;
					main.startColor = new ParticleSystem.MinMaxGradient(startColor);
				}
			}
		}
	}

	private IEnumerator LateFixedUpdate()
	{
		while (enabled)
		{
			yield return new WaitForFixedUpdate();

			if (Time.time - _initTime < _config.GuidanceActivationDelay) continue;

			switch (_config.GuidanceAlgorithm)
			{
				case MissileGuidanceAlgorithm.DeadFire:
					_targetAcceleration = transform.up * _config.MaxAcceleration;
					break;
				case MissileGuidanceAlgorithm.Predictive:
					SolvePredictiveGuidance();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}

	private void SolvePredictiveGuidance()
	{
		if (_targetBody == null)
		{
			_targetAcceleration = transform.up * _config.MaxAcceleration;
		}
		else
		{
			Vector2 relativePosition = _targetBody.worldCenterOfMass - _body.worldCenterOfMass;
			Vector2 relativeVelocity = _targetBody.velocity - _body.velocity;

			if (
				InterceptSolver.MissileIntercept(
					relativePosition, relativeVelocity, _config.MaxAcceleration,
					out Vector2 acceleration, out float hitTime
				)
			)
			{
				_targetAcceleration = acceleration;
			}
			else
			{
				_targetAcceleration = transform.up * _config.MaxAcceleration;
			}
		}
	}

	private float GetHealthDamageModifier()
	{
		return _pdTarget == null
			? 1f
			: MathUtils.Remap(
				_pdTarget.HealthFraction,
				0f, 1f, 1f - _config.HealthDamageScaling, 1f
			);
	}

	private void EndOfLifeDespawn()
	{
		GetComponent<DamagingProjectile>().OnLifetimeDespawn();
	}

	private void OnDrawGizmos()
	{
		if (_targetAcceleration != null)
		{
			Gizmos.color = Color.red;
			Gizmos.matrix = Matrix4x4.identity;
			Gizmos.DrawLine(transform.position, transform.position + (Vector3) _targetAcceleration.Value);
		}
	}
}
}