using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Block
{
public class VolatileSpec
{
	// Information only - up to the exact components of the block to control explosion behaviour when the block is actually destroyed.
	public bool AlwaysExplode;
	public Vector2 ExplosionOffset;
	public float MaxRadius;
	public float MaxDamage;
}
}