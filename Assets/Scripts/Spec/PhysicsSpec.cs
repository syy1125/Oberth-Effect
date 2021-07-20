using UnityEngine;
using Vector2 = System.Numerics.Vector2;

namespace Syy1125.OberthEffect.Spec
{
public struct PhysicsSpec
{
	public Vector2Int BoundsMin;
	public Vector2Int BoundsMax;
	public Vector2Int[] AttachmentPoints;

	public Vector2 CenterOfMass;
	public float Mass;
	public float MomentOfInertia;
}
}