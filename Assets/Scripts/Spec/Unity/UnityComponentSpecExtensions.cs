using Syy1125.OberthEffect.Common.Colors;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Unity
{
public static class UnityComponentSpecExtensions
{
	public static void LoadSpec(this Transform transform, RendererSpec spec)
	{
		transform.localPosition = new Vector3(spec.Offset.x, spec.Offset.y, 0f);
		transform.localRotation = Quaternion.AngleAxis(spec.Rotation, Vector3.forward);
		transform.localScale = new Vector3(spec.Scale.x, spec.Scale.y, 1f);
	}

	public static void LoadSpec(this SpriteRenderer renderer, RendererSpec spec)
	{
		renderer.sprite = TextureDatabase.Instance.GetSprite(spec.TextureId);

		if (TextureDatabase.Instance.GetTextureSpec(spec.TextureId).Spec.ApplyVehicleColors)
		{
			renderer.sharedMaterial = TextureDatabase.Instance.VehicleBlockMaterial;
		}
	}

	public static void LoadSpec(this BoxCollider2D collider, BoxColliderSpec spec)
	{
		collider.offset = spec.Offset;
		collider.size = spec.Size;
	}

	public static void LoadSpec(this PolygonCollider2D collider, PolygonColliderPathSpec[] spec)
	{
		collider.pathCount = spec.Length;
		for (int i = 0; i < spec.Length; i++)
		{
			collider.SetPath(i, spec[i].Path);
		}
	}

	public static void LoadSpec(this ParticleSystem particles, ParticleSystemSpec spec)
	{
		var main = particles.main;
		main.startSize = spec.Size;
		main.startLifetime = spec.Lifetime;
		main.playOnAwake = false;

		var colorContext = particles.GetComponentInParent<ColorContext>();
		if (colorContext.ColorScheme.ResolveColor(spec.Color, out Color startColor))
		{
			main.startColor = startColor;
		}
		else
		{
			Debug.LogError($"Failed to parse particle color {spec.Color}");
		}

		var emission = particles.emission;
		emission.enabled = true;
		emission.rateOverTime = spec.EmissionRateOverTime;
		emission.rateOverDistance = spec.EmissionRateOverDistance;

		var shape = particles.shape;
		shape.enabled = true;
		shape.angle = spec.SpreadAngle;
		shape.radius = 0f;

		var colorLifetime = particles.colorOverLifetime;
		colorLifetime.enabled = true;
		var gradient = new Gradient();
		gradient.SetKeys(
			new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
			new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
		);
		colorLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

		var renderer = particles.GetComponent<Renderer>();
		renderer.enabled = true;
		renderer.material = TextureDatabase.Instance.DefaultParticleMaterial;
	}
}
}