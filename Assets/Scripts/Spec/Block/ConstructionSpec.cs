using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Block
{
public struct ConstructionSpec
{
	public bool ShowInDesigner;
	public bool AllowErase;

	public Vector2Int BoundsMin;
	public Vector2Int BoundsMax;
	public Vector2Int[] AttachmentPoints;
}
}