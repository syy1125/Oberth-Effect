﻿using System;
using System.Collections;
using Photon.Pun;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Common.Physics;
using Syy1125.OberthEffect.Common.Utils;
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
	public float ThrustActivationDelay;
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

	private Rigidbody2D _ownBody;
	private MissileConfig _config;
	private TargetLockTarget _target;
	private PointDefenseTarget _pdTarget;
	private ParticleSystem[] _propulsionParticles;
	private float[] _maxParticleSpeeds;

	private float _initTime;
	private Vector2? _targetAcceleration;
	private Pid<float> _rotationPid;

	private void Awake()
	{
		_ownBody = GetComponent<Rigidbody2D>();
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
			_pdTarget.OnDestroyedByDamage.AddListener(EndOfLifeDespawn);

			gameObject.AddComponent<ReferenceFrameProvider>();
			var radiusProvider = gameObject.AddComponent<ConstantCollisionRadiusProvider>();
			radiusProvider.Radius = _config.ColliderSize.magnitude / 2;
		}

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
			_target = PhotonView.Find(_config.TargetPhotonId)?.GetComponent<TargetLockTarget>();
			Debug.Log($"Target is {_target}");
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
		if (_targetAcceleration == null)
		{
			if (Time.time - _initTime >= _config.ThrustActivationDelay)
			{
				ApplyThrust(_config.MaxAcceleration);
			}
			else
			{
				ApplyThrust(0f);
			}
		}
		else
		{
			float angle = Vector2.SignedAngle(_targetAcceleration.Value, transform.up);
			_rotationPid.Update(angle, Time.fixedDeltaTime);

			float rotationResponse = Mathf.Clamp(_rotationPid.Output, -1f, 1f);
			_ownBody.angularVelocity -= rotationResponse * _config.MaxAngularAcceleration * Time.fixedDeltaTime;

			if (Time.time - _initTime >= _config.ThrustActivationDelay)
			{
				float cos = Mathf.Clamp01(Mathf.Cos(angle * Mathf.Deg2Rad));
				float thrustFraction = cos * cos;
				ApplyThrust(thrustFraction * _targetAcceleration.Value.magnitude);
			}
		}
	}

	private void ApplyThrust(float acceleration)
	{
		_ownBody.velocity += (Vector2) transform.up * (acceleration * Time.deltaTime);

		float thrustScale = acceleration / _config.MaxAcceleration;

		if (_propulsionParticles != null)
		{
			ParticleSystemUtils.ScaleThrustParticles(_propulsionParticles, thrustScale);
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
		if (_target == null)
		{
			_targetAcceleration = transform.up * _config.MaxAcceleration;
		}
		else
		{
			Vector2 relativePosition = _target.GetEffectivePosition() - _ownBody.worldCenterOfMass;
			Vector2 relativeVelocity = _target.GetEffectiveVelocity() - _ownBody.velocity;

			if (
				InterceptSolver.MissileIntercept(
					relativePosition, relativeVelocity, _config.MaxAcceleration,
					out Vector2 acceleration, out float hitTime
				)
			)
			{
				_targetAcceleration = acceleration;
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
			Gizmos.matrix = Matrix4x4.identity;
			Gizmos.color = Color.green;
			Gizmos.DrawLine(transform.position, transform.position + (Vector3) _targetAcceleration.Value);
			Gizmos.color = new Color(1f, 0.5f, 0f);
			float angle = Vector2.SignedAngle(_targetAcceleration.Value, transform.up);
			float cos = Mathf.Clamp01(Mathf.Cos(angle * Mathf.Deg2Rad));
			Gizmos.DrawLine(
				transform.position,
				transform.TransformPoint(new Vector2(0f, cos * cos * _targetAcceleration.Value.magnitude))
			);
		}
	}
}
}