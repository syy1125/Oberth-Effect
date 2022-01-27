using System.Collections.Generic;
using Syy1125.OberthEffect.Common.Colors;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Unity
{
// ParticleSystem.MainModule.*Multiplier actually only works in curve mode. In constant mode, they take on the value of the constant.
// This is "not a big". See https://fogbugz.unity3d.com/default.asp?1192367_24ij9pq00vgu8f5d
// This component is created as a workaround to make "*Multiplier" actually multipliers.
[RequireComponent(typeof(ParticleSystem))]
public class ParticleSystemWrapper : MonoBehaviour
{
	private ParticleSystem _psCache;

	public ParticleSystem Particles
	{
		get
		{
			if (_psCache == null)
			{
				_psCache = GetComponent<ParticleSystem>();
			}

			return _psCache;
		}
	}

	private float _startSpeed;

	public void LoadSpec(ParticleSystemSpec spec)
	{
		var main = Particles.main;
		main.startSize = spec.Size;
		main.startLifetime = spec.Lifetime;
		main.playOnAwake = false;

		_startSpeed = spec.MaxSpeed;
		main.startSpeed = _startSpeed;

		var colorContext = GetComponentInParent<ColorContext>();
		if (colorContext.ColorScheme.ResolveColor(spec.Color, out Color startColor))
		{
			main.startColor = startColor;
		}
		else
		{
			Debug.LogError($"Failed to parse particle color {spec.Color}");
		}

		var emission = Particles.emission;
		emission.enabled = true;
		emission.rateOverTime = spec.EmissionRateOverTime;
		emission.rateOverDistance = spec.EmissionRateOverDistance;

		var shape = Particles.shape;
		shape.enabled = true;
		shape.angle = spec.SpreadAngle;
		shape.radius = 0f;

		var colorLifetime = Particles.colorOverLifetime;
		colorLifetime.enabled = true;
		var gradient = new Gradient();
		gradient.SetKeys(
			new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
			new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
		);
		colorLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

		var particleRenderer = GetComponent<Renderer>();
		particleRenderer.enabled = true;
		particleRenderer.material = TextureDatabase.Instance.DefaultParticleMaterial;
	}

	public void Play()
	{
		Particles.Play();
	}

	public void Stop()
	{
		Particles.Stop();
	}

	public static void BatchScaleThrustParticles(IEnumerable<ParticleSystemWrapper> wrappers, float thrustScale)
	{
		foreach (ParticleSystemWrapper wrapper in wrappers)
		{
			var main = wrapper.Particles.main;

			main.startSpeedMultiplier = wrapper._startSpeed * thrustScale;
			var startColor = main.startColor.color;
			startColor.a = thrustScale;
			main.startColor = startColor;
		}
	}
}
}