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

	public static ParticleSystem CreateParticleSystem(Transform parent, ParticleSystemSpec spec)
	{
		GameObject particleHolder = new GameObject("ParticleSystem");

		var holderTransform = particleHolder.transform;
		holderTransform.SetParent(parent);
		holderTransform.localPosition = new Vector3(spec.Offset.x, spec.Offset.y, 1f);
		holderTransform.localRotation = Quaternion.LookRotation(spec.Direction);

		var particles = particleHolder.AddComponent<ParticleSystem>();
		particles.LoadSpec(spec);
		particles.Stop();
		return particles;
	}
}
}