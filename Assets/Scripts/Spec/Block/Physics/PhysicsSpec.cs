using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Block.Physics
{
public struct PhysicsSpec
{
	public Vector2 CenterOfMass;
	public float Mass;
	public float MomentOfInertia;

	public BoxColliderSpec BoxCollider;
	public PolygonColliderPathSpec[] PolygonCollider;
}
}