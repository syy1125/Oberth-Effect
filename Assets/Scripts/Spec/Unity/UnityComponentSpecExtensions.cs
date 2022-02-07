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
}
}