using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;

namespace Syy1125.OberthEffect.Prototyping
{
public class InterceptTest : MonoBehaviour
{
	public Transform Target;
	public Transform T1;
	public float Speed = 2f;

	private void OnDrawGizmos()
	{
		if (Target == null || T1 == null) return;

		Vector2 targetVelocity = T1.position - Target.position;
		bool hit = InterceptSolver.ProjectileIntercept(
			Target.position, targetVelocity, Speed,
			out Vector2 projectileVelocity, out float time
		);

		Gizmos.matrix = Matrix4x4.identity;
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(Target.position + (Vector3) targetVelocity * time, 0.5f);

		Gizmos.color = hit ? Color.cyan : Color.red;
		Gizmos.DrawWireSphere(projectileVelocity * time, 0.5f);
	}
}
}