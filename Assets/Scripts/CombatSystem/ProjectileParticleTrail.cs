using System.Linq;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;

namespace Syy1125.OberthEffect.CombatSystem
{
public class ProjectileParticleTrail : MonoBehaviour, IProjectileLifecycleListener
{
	private ParticleSystemWrapper[] _particles;
	private float _particleLifetime;

	public void LoadTrailParticles(ParticleSystemSpec[] trailParticles)
	{
		_particles = RendererHelper.CreateParticleSystems(transform, trailParticles);
		_particleLifetime = trailParticles.Max(spec => spec.Lifetime);

		var mainCameraTransform = Camera.main.transform;

		foreach (ParticleSystemWrapper particle in _particles)
		{
			ParticleSystem.MainModule main = particle.ParticleSystem.main;
			main.simulationSpace = ParticleSystemSimulationSpace.Custom;
			main.customSimulationSpace = mainCameraTransform;
			
			// `RendererHelper.CreateParticleSystem` creates particle systems in the stopped states. In order for `ParticleSystem.Simulate` to work correctly, the particle system needs to be in a paused state.
			particle.Play();
			particle.Pause();
		}
	}

	private void FixedUpdate()
	{
		foreach (ParticleSystemWrapper particle in _particles)
		{
			particle.ParticleSystem.Simulate(Time.fixedDeltaTime, false, false);
		}
	}

	public void AfterSpawn()
	{}

	public void BeforeDespawn()
	{
		PersistParticles();
	}

	private void PersistParticles()
	{
		GameObject persistParticles = new GameObject("PersistProjectileParticles");

		foreach (ParticleSystemWrapper particle in _particles)
		{
			particle.ParticleSystem.Simulate(Time.fixedDeltaTime, false, false);

			particle.transform.SetParent(persistParticles.transform);
			var emission = particle.ParticleSystem.emission;
			emission.rateOverTime = 0f;
			emission.rateOverDistance = 0f;
			particle.ParticleSystem.Play();
		}

		Destroy(persistParticles, _particleLifetime);
	}
}
}