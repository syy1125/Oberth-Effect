using UnityEngine;

namespace Syy1125.OberthEffect.Common.ColorScheme
{
[RequireComponent(typeof(Renderer))]
public class GlowReferenceTransform : MonoBehaviour
{
	private static readonly int GlowReference = Shader.PropertyToID("_GlowReference");
	private static readonly int GlowRange = Shader.PropertyToID("_GlowRange");

	private Renderer _renderer;
	private MaterialPropertyBlock _block;

	private void Awake()
	{
		_renderer = GetComponent<Renderer>();
		_block = new MaterialPropertyBlock();
	}

	private void LateUpdate()
	{
		Matrix4x4 referenceTransform = _renderer.worldToLocalMatrix;

		foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>())
		{
			sprite.GetPropertyBlock(_block);
			_block.SetMatrix(GlowReference, referenceTransform);
			_block.SetVector(GlowRange, new Vector4(.9f, 1.1f));
			sprite.SetPropertyBlock(_block);
		}
	}
}
}