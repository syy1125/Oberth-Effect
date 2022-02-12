using Syy1125.OberthEffect.Foundation.Physics;
using UnityEngine;

namespace Syy1125.OberthEffect.Foundation
{
public class ConstantCollisionRadiusProvider : MonoBehaviour, ICollisionRadiusProvider
{
	public float Radius;

	public float GetCollisionRadius()
	{
		return Radius;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.magenta;
		Gizmos.matrix = Matrix4x4.identity;
		Gizmos.DrawWireSphere(transform.position, Radius);
	}
}
}