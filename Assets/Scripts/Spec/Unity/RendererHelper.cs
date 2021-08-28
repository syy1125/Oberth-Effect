using System.Collections.Generic;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Unity
{
public static class RendererHelper
{
	public static List<SpriteRenderer> AttachRenderers(Transform parent, IEnumerable<RendererSpec> renderers)
	{
		List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();

		foreach (RendererSpec rendererSpec in renderers)
		{
			if (!TextureDatabase.Instance.HasTexture(rendererSpec.TextureId)) continue;

			var rendererObject = new GameObject("Renderer");

			var rendererTransform = rendererObject.transform;
			rendererTransform.SetParent(parent);
			rendererTransform.LoadSpec(rendererSpec);

			var spriteRenderer = rendererObject.AddComponent<SpriteRenderer>();
			spriteRenderer.LoadSpec(rendererSpec);
			spriteRenderers.Add(spriteRenderer);
		}

		return spriteRenderers;
	}
}
}