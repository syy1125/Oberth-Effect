using System.Collections.Generic;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Unity
{
public static class RendererHelper
{
	public static void AttachRenderers(Transform parent, IEnumerable<RendererSpec> renderers)
	{
		foreach (RendererSpec rendererSpec in renderers)
		{
			if (!TextureDatabase.Instance.ContainsId(rendererSpec.TextureId)) continue;

			var rendererObject = new GameObject("Renderer");

			var rendererTransform = rendererObject.transform;
			rendererTransform.SetParent(parent);
			rendererTransform.LoadSpec(rendererSpec);

			var spriteRenderer = rendererObject.AddComponent<SpriteRenderer>();
			spriteRenderer.LoadSpec(rendererSpec);
		}
	}

	public static ParticleSystemWrapper CreateParticleSystem(Transform parent, ParticleSystemSpec spec)
	{
		GameObject particleHolder = new GameObject("ParticleSystem");

		var holderTransform = particleHolder.transform;
		holderTransform.SetParent(parent);
		holderTransform.localPosition = new(spec.Offset.x, spec.Offset.y, 1f);
		holderTransform.localRotation = Quaternion.LookRotation(spec.Direction);

		var particles = particleHolder.AddComponent<ParticleSystem>();
		var wrapper = particleHolder.AddComponent<ParticleSystemWrapper>();
		wrapper.LoadSpec(spec);
		particles.Stop();
		return wrapper;
	}

	public static ParticleSystemWrapper[] CreateParticleSystems(Transform parent, ParticleSystemSpec[] specs)
	{
		ParticleSystemWrapper[] particles = new ParticleSystemWrapper[specs.Length];

		for (int i = 0; i < specs.Length; i++)
		{
			particles[i] = CreateParticleSystem(parent, specs[i]);
		}

		return particles;
	}
}
}