using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;
using Syy1125.OberthEffect.Spec.Unity;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Block
{
[CreateSchemaFile("PhysicsSpecSchema")]
public struct PhysicsSpec
{
	public Vector2 CenterOfMass;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float Mass;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MomentOfInertia;

	public BoxColliderSpec BoxCollider;
	public PolygonColliderPathSpec[] PolygonCollider;
}
}