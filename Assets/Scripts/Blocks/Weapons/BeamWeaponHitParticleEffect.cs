using System;
using Syy1125.OberthEffect.Common.ColorScheme;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Weapons
{
[RequireComponent(typeof(ParticleSystem))]
public class BeamWeaponHitParticleEffect : MonoBehaviour, IBeamWeaponVisualEffect
{
	private ParticleSystem _particles;

	private void Awake()
	{
		_particles = GetComponent<ParticleSystem>();
	}

	public void SetColorScheme(ColorScheme colors)
	{
		var main = _particles.main;
		main.startColor = new ParticleSystem.MinMaxGradient(colors.PrimaryColor);
	}

	public void SetBeamPoints(Vector3 begin, Vector3 end, bool hit)
	{
		var emission = _particles.emission;
		var shape = _particles.shape;

		if (hit)
		{
			emission.enabled = true;
			shape.position = transform.InverseTransformPoint(end);
		}
		else
		{
			emission.enabled = false;
		}
	}
}
}