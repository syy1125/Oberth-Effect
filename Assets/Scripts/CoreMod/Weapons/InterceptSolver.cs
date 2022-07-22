using Syy1125.OberthEffect.Lib.Math;
using UnityEngine;

namespace Syy1125.OberthEffect.CoreMod.Weapons
{
public static class InterceptSolver
{
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
		float dot = Vector2.Dot(targetPosition, targetVelocity); // dot = b/2

		float diff = v2 - vp2; // diff = a
		float inner = dot * dot - r2 * diff; // inner = 1/4 (b^2 - 4ac)

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
				// This means that the target is faster than the projectile and it's moving away.
				// Essentially the same as the "No solution" case, use same technique for best effort closest approach.
				float sqrt = Mathf.Sqrt((-dot * dot + r2 * v2) * vp2 * diff);
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

			projectileVelocity = (targetPosition + targetVelocity * hitTime) / hitTime;

			Debug.Assert(
				Mathf.Abs(projectileVelocity.magnitude - projectileSpeed) < 0.01f,
				$"Output velocity {projectileVelocity} should have same magnitude as input speed {projectileSpeed}"
			);

			return true;
		}
	}

	public static bool MissileIntercept(
		Vector2 targetPosition, Vector2 targetVelocity, float missileAcceleration,
		out Vector2 accelerationVector, out float hitTime
	)
	{
		var marginExpression = new PolynomialExpression(
			4 * targetPosition.sqrMagnitude,
			8 * Vector2.Dot(targetPosition, targetVelocity),
			4 * targetVelocity.sqrMagnitude,
			0f,
			-missileAcceleration * missileAcceleration
		);

		// Find a ballpark estimate of the intercept time and use it as seed for the Halley solver.
		float seed = FindMissileInterceptSeed(
			targetPosition, targetVelocity, missileAcceleration, marginExpression
		);
		hitTime = HalleySolver.FindRoot(marginExpression, seed, out bool converged);

		Vector2 hitPosition = targetPosition + targetVelocity * hitTime;
		accelerationVector = hitPosition.normalized * missileAcceleration;

		return converged && hitTime > 0f;
	}

	private static float FindMissileInterceptSeed(
		Vector2 targetPosition, Vector2 targetVelocity, float missileAcceleration, IExpression marginExpression
	)
	{
		float accelerationTime = Mathf.Sqrt(2 * targetPosition.magnitude / missileAcceleration);
		float velocityTime = -targetPosition.sqrMagnitude / Vector2.Dot(targetPosition, targetVelocity);

		if (velocityTime < Mathf.Epsilon)
		{
			// This implies that the target is moving away, so acceleration has to be dominant.
			// Correct for velocity once, then the seed should be good enough for Halley solver to converge.
			Vector2 correctedPosition = targetPosition + targetVelocity * accelerationTime;
			return Mathf.Sqrt(2 * correctedPosition.magnitude / missileAcceleration);
		}
		else
		{
			// The dominant term should naturally become dominant with this expression;
			return 1 / (1 / velocityTime + 1 / accelerationTime);
		}
	}

	private static float FindMissileInterceptSeedInterval(
		IExpression marginExpression, float scaleTime, float targetInterval = 0.1f
	)
	{
		// Step through intervals from 0-2 times the scale time to try to find an interval that crosses from positive to negative.
		float stepSize = scaleTime / 10f;
		int i = 1;

		for (; i <= 20; i++)
		{
			if (marginExpression.Evaluate(stepSize * i) < 0)
			{
				// If the interval is found, narrow down until the interval is less than targetInterval
				float min = stepSize * (i - 1), max = stepSize * i;

				while (max - min > targetInterval)
				{
					float mid = (min + max) / 2;

					if (marginExpression.Evaluate(mid) > 0)
					{
						min = mid;
					}
					else
					{
						max = mid;
					}
				}

				return (min + max) / 2;
			}
		}

		Debug.LogWarning($"Failed to find seed for {marginExpression}. Using scaleTime {scaleTime} as fallback.");
		return scaleTime;
	}

	#region Internal Utlity Methods

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

	#endregion
}
}