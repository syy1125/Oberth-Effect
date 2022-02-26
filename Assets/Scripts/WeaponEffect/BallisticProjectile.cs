using System;
using System.Linq;
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
public class BallisticProjectile : MonoBehaviourPun, IPunInstantiateMagicCallback, IProjectileDespawnListener
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
			GetHealthDamageModifier, _config.Lifetime
		);

		GetComponent<BoxCollider2D>().size = _config.ColliderSize;

		if (_config.IsPointDefenseTarget)
		{
			_pdTarget = gameObject.AddComponent<PointDefenseTarget>();
			_pdTarget.Init(_config.MaxHealth, _config.ArmorValue, _config.ColliderSize);
			_pdTarget.OnDestroyedByDamage.AddListener(GetComponent<DamagingProjectile>().LifetimeDespawn);

			gameObject.AddComponent<ReferenceFrameProvider>();
			var radiusProvider = gameObject.AddComponent<ConstantCollisionRadiusProvider>();
			radiusProvider.Radius = _config.ColliderSize.magnitude / 2;
		}

		RendererHelper.AttachRenderers(transform, _config.Renderers);

		if (_config.TrailParticles != null)
		{
			_particles = RendererHelper.CreateParticleSystems(transform, _config.TrailParticles);
			var mainCameraTransform = Camera.main.transform;

			foreach (ParticleSystemWrapper particle in _particles)
			{
				var main = particle.ParticleSystem.main;
				main.simulationSpace = ParticleSystemSimulationSpace.Custom;
				main.customSimulationSpace = mainCameraTransform;
			}
		}
	}

	private void Start()
	{
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

	public void BeforeDespawn()
	{
		PersistParticles();
	}

	private void PersistParticles()
	{
		if (_particles != null && _particles.Length > 0)
		{
			GameObject persistParticles = new GameObject("PersistProjectileParticles");

			foreach (ParticleSystemWrapper particle in _particles)
			{
				particle.ParticleSystem.Simulate(Time.fixedDeltaTime, true, false);

				particle.transform.SetParent(persistParticles.transform);
				var emission = particle.ParticleSystem.emission;
				emission.rateOverTime = 0f;
				emission.rateOverDistance = 0f;
				particle.ParticleSystem.Play();
			}

			Destroy(persistParticles, _config.TrailParticles.Select(spec => spec.Lifetime).Max());
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
}
}