using System;
using Syy1125.OberthEffect.CombatSystem;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Prototyping
{
public class Playground : MonoBehaviour
{
	private void Start()
	{
		GetComponent<Rigidbody2D>().velocity = Vector2.right;
		GetComponent<ProjectileParticleTrail>().LoadTrailParticles(
			new[]
			{
				new ParticleSystemSpec
				{
					Direction = Vector2.down,
					SpreadAngle = 180,
					EmissionRateOverTime = 0,
					EmissionRateOverDistance = 25,
					Size = 0.5f,
					MaxSpeed = 1.5f,
					Lifetime = 0.5f,
					Color = "green"
				}
			}
		);
	}
}
}