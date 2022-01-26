using UnityEngine;

namespace Syy1125.OberthEffect.Common.Utils
{
public static class ParticleSystemUtils
{
	public static void ScaleThrustParticles(ParticleSystem[] particleSystems, float thrustScale)
	{
		foreach (ParticleSystem particle in particleSystems)
		{
			var main = particle.main;
			main.startSpeedMultiplier = thrustScale;
			Color startColor = main.startColor.color;
			startColor.a = thrustScale;
			main.startColor = new ParticleSystem.MinMaxGradient(startColor);
		}
	}
}
}