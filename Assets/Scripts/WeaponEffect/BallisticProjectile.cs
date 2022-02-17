using System;
using Photon.Pun;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Foundation.Physics;
using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
[Serializable]
public struct ProjectileConfig
{
	public Vector2 ColliderSize;
	public float Damage;
	public DamageType DamageType;
	public float ArmorPierce; // Note that explosive damage will always have armor pierce of 1
	public float ExplosionRadius; // Only relevant for explosive damage
	public float Lifetime;

	public bool IsPointDefenseTarget;
	public float MaxHealth;
	public float ArmorValue;
	public float HealthDamageScaling;

	public RendererSpec[] Renderers;
	public ParticleSystemSpec[] TrailParticles;
}

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(DamagingProjectile))]
public class BallisticProjectile : MonoBehaviourPun, IPunInstantiateMagicCallback
{
	private ProjectileConfig _config;
	private PointDefenseTarget _pdTarget;
	private ParticleSystemWrapper[] _particles;

	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		object[] instantiationData = info.photonView.InstantiationData;
		_config = JsonUtility.FromJson<ProjectileConfig>(
			CompressionUtils.Decompress((byte[]) instantiationData[0])
		);

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

		if (_config.TrailParticles != null)
		{
			_particles = RendererHelper.CreateParticleSystems(transform, _config.TrailParticles);

			foreach (ParticleSystemWrapper particle in _particles)
			{
				var main = particle.ParticleSystem.main;
				main.simulationSpace = ParticleSystemSimulationSpace.Custom;
				main.customSimulationSpace = Camera.main.transform;
			}
		}
	}

	private void Start()
	{
		Invoke(nameof(EndOfLifeDespawn), _config.Lifetime);

		if (_particles != null)
		{
			foreach (ParticleSystemWrapper particle in _particles)
			{
				particle.ParticleSystem.Simulate(Time.fixedDeltaTime);
			}
		}
	}

	private void FixedUpdate()
	{
		if (_particles != null)
		{
			foreach (ParticleSystemWrapper particle in _particles)
			{
				particle.ParticleSystem.Simulate(Time.fixedDeltaTime, true, false);
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
		GetComponent<DamagingProjectile>().DetonateOrDespawn();
	}
}
}