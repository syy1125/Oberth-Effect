using System;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
public static class InterceptSolver
{
	private const int MAX_ITERATIONS = 5;
	private const float SOLVE_THRESHOLD = 0.1f;

	/// <returns>Whether an intercept is found. If false, the output values are best-effort closest approach solutions.</returns>
	public static bool ProjectileIntercept(
		Vector2 targetPosition, Vector2 targetVelocity, float projectileSpeed,
		out Vector2 projectileVelocity, out float hitTime
	)
	{
		projectileVelocity = Vector2.zero;
		hitTime = 0f;

		// Parameterize the position of the target as a function of time. To minimize the projectile's distance to the target
		// at the designated time, obviously the projectile's velocity should line up with the target position at the designated time.
		// r(t) = r0 + vt, d = | |r(t)| - vp t |

		float r2 = targetPosition.sqrMagnitude;
		float v2 = targetVelocity.sqrMagnitude;
		float vp2 = projectileSpeed * projectileSpeed;
		float dot = Vector2.Dot(targetPosition, targetVelocity);

		float diff = v2 - vp2;
		float inner = dot * dot - r2 * diff;

		if (Mathf.Approximately(diff, 0f)) // Singularity
		{
			// Singularity means projectile and target velocities match.
			// But since we know that now, we can solve this differently.
			if (dot >= 0)
			{
				// Target is travelling away and can never be hit
				hitTime = float.PositiveInfinity;
				projectileVelocity = targetVelocity.normalized * projectileSpeed;
				return false;
			}
			else
			{
				hitTime = -r2 / (2 * dot);
				projectileVelocity = (targetPosition + targetVelocity * hitTime) / hitTime;

				Debug.Assert(
					Mathf.Approximately(projectileVelocity.magnitude, projectileSpeed),
					$"Output velocity {projectileVelocity} should have same magnitude as input speed {projectileSpeed}"
				);

				return true;
			}
		}
		else if (inner < Mathf.Epsilon) // No solution
		{
			// Do best effort closest approach
			// If we reach this branch, it means that diff must be positive. Therefore we can expect this to not error.
			float sqrt = Mathf.Sqrt((-dot * dot + r2 * v2) * vp2 * diff);

			// Not entirely sure about this, but it looks like although Mathematica lists two solutions,
			// because of how the minimization equation is solved by squaring both sides, one of the solutions actually
			// isn't a solution because the sign got flipped around when solving.
			hitTime = (-dot * diff + sqrt) / (v2 * diff);

			if (hitTime < Mathf.Epsilon)
			{
				hitTime = 0f;
				projectileVelocity = targetPosition.normalized * projectileSpeed;
				return false;
			}

			projectileVelocity = (targetPosition + targetVelocity * hitTime).normalized * projectileSpeed;

			return false;
		}
		else // Yes solution
		{
			float t1 = (-dot + Mathf.Sqrt(inner)) / diff;
			float t2 = (-dot - Mathf.Sqrt(inner)) / diff;

			if (!BoundedMin(t1, t2, Mathf.Epsilon, out hitTime))
			{
				projectileVelocity = targetPosition.normalized * projectileSpeed;
				return false;
			}

			projectileVelocity = (targetPosition + targetVelocity * hitTime) / hitTime;

			Debug.Assert(
				Mathf.Abs(projectileVelocity.magnitude - projectileSpeed) < 0.01f,
				$"Output velocity {projectileVelocity} should have same magnitude as input speed {projectileSpeed}"
			);

			return true;
		}
	}

	private static bool BoundedMin(float left, float right, float min, out float result)
	{
		if (left < min)
		{
			if (right < min)
			{
				result = min;
				return false;
			}
			else
			{
				result = right;
				return true;
			}
		}
		else
		{
			if (right < min)
			{
				result = left;
				return true;
			}
			else
			{
				result = Mathf.Min(left, right);
				return true;
			}
		}
	}
}
}