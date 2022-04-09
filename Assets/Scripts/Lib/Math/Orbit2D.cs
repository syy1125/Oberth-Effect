using System;
using Syy1125.OberthEffect.Lib.Utils;
using UnityEngine;

namespace Syy1125.OberthEffect.Lib.Math
{
[Serializable]
public class Orbit2D
{
	public float ParentGravitationalParameter;
	// Semi-Latus Rectum is used instead of Semi-Major Axis to avoid singularities in the parabolic case.
	public float SemiLatusRectum;
	public float Eccentricity;
	public float ArgumentOfPeriapsis;
	public float TrueAnomalyAtEpoch;

	// Config variables
	private const int SOLVER_ITERATION_LIMIT = 10;
	private const float SOLVER_SMALL_THRESHOLD = 1e-15f;

	private float GetTrueAnomalyAt(float time)
	{
		if (Mathf.Approximately(time, 0f))
		{
			return TrueAnomalyAtEpoch;
		}

		float meanMotion = Mathf.Sqrt(
			ParentGravitationalParameter
			* Mathf.Pow(Mathf.Abs(1 - Mathf.Pow(Eccentricity, 2)) / SemiLatusRectum, 3)
		);

		if (Mathf.Approximately(Eccentricity, 1f)) // Parabolic case
		{
			// Reference: https://en.wikipedia.org/wiki/Parabolic_trajectory#Barker's_equation

			// Solve for time of periapsis passage
			float parabolicAnomalyAtEpoch = Mathf.Tan(TrueAnomalyAtEpoch / 2);
			float periapsisTime = -Mathf.Sqrt(Mathf.Pow(SemiLatusRectum, 3) / ParentGravitationalParameter)
			                      / 2
			                      * (parabolicAnomalyAtEpoch + Mathf.Pow(parabolicAnomalyAtEpoch, 3) / 2);

			// Use the substitutions given in the reference
			float a = 3f
			          * Mathf.Sqrt(ParentGravitationalParameter / Mathf.Pow(SemiLatusRectum, 3))
			          * (time - periapsisTime);
			float b = Mathf.Pow(a + Mathf.Sqrt(Mathf.Pow(a, 2) + 1), 1 / 3f);
			return 2 * Mathf.Atan(b - 1 / b);
		}
		else if (Eccentricity < 1) // Elliptic case
		{
			// References:
			// https://en.wikipedia.org/wiki/Mean_anomaly#Formulae
			// https://en.wikipedia.org/wiki/True_anomaly#From_the_eccentric_anomaly

			float eccentricAnomalyAtEpoch =
				2
				* Mathf.Atan2(
					Mathf.Sqrt((1 - Eccentricity) / (1 + Eccentricity)) * Mathf.Sin(TrueAnomalyAtEpoch / 2),
					Mathf.Cos(TrueAnomalyAtEpoch / 2)
				);

			float meanAnomalyAtEpoch = eccentricAnomalyAtEpoch - Eccentricity * Mathf.Sin(eccentricAnomalyAtEpoch);
			float meanAnomaly = (meanAnomalyAtEpoch + meanMotion * time) % (2 * Mathf.PI);

			// Use Newton's solver
			float eccentricAnomaly = meanAnomaly;
			for (var i = 0; i < SOLVER_ITERATION_LIMIT; i++)
			{
				float dE = -(eccentricAnomaly - Eccentricity * Mathf.Sin(eccentricAnomaly) - meanAnomaly)
				           / (1 - Eccentricity * Mathf.Cos(eccentricAnomaly));
				eccentricAnomaly += dE;
				if (Mathf.Abs(dE) < SOLVER_SMALL_THRESHOLD) break;
			}

			return 2
			       * Mathf.Atan2(
				       Mathf.Sqrt(1 + Eccentricity) * Mathf.Sin(eccentricAnomaly / 2),
				       Mathf.Sqrt(1 - Eccentricity) * Mathf.Cos(eccentricAnomaly / 2)
			       );
		}
		else // Hyperbolic case
		{
			// Reference: https://en.wikipedia.org/wiki/Hyperbolic_trajectory#Equations_of_motion
			float hyperbolicAnomalyAtEpoch = 2
			                                 * MathUtils.Atanh(
				                                 Mathf.Sqrt((Eccentricity - 1) / (Eccentricity + 1))
				                                 * Mathf.Tan(TrueAnomalyAtEpoch / 2)
			                                 );
			float meanAnomalyAtEpoch =
				Eccentricity * (float) System.Math.Sinh(hyperbolicAnomalyAtEpoch) - hyperbolicAnomalyAtEpoch;

			float meanAnomaly = meanAnomalyAtEpoch + meanMotion * time;

			// Use Newton's solver
			// Initial value estimate provided by Danby, Fundamentals of Celesital Mechanics (p.176)
			float hyperbolicAnomaly = Mathf.Log(2 * meanAnomaly / Eccentricity + 1.8f);
			for (var i = 0; i < SOLVER_ITERATION_LIMIT; i++)
			{
				float dH = -(Eccentricity * (float) System.Math.Sinh(hyperbolicAnomaly)
				             - hyperbolicAnomaly
				             - meanAnomaly)
				           / (Eccentricity * (float) System.Math.Cosh(hyperbolicAnomaly) - 1);
				hyperbolicAnomaly += dH;
				if (Mathf.Abs(dH) < SOLVER_SMALL_THRESHOLD) break;
			}

			return 2
			       * Mathf.Atan2(
				       Mathf.Sqrt(Eccentricity + 1) * (float) System.Math.Sinh(hyperbolicAnomaly / 2),
				       Mathf.Sqrt(Eccentricity - 1) * (float) System.Math.Cosh(hyperbolicAnomaly / 2)
			       );
		}
	}

	private float GetRadiusAt(float trueAnomaly)
	{
		return SemiLatusRectum / (1 + Eccentricity * Mathf.Cos(trueAnomaly));
	}

	public Tuple<Vector2, Vector2> GetStateVectorAt(float time)
	{
		float trueAnomaly = GetTrueAnomalyAt(time);
		float radius = GetRadiusAt(trueAnomaly);

		// Compute position and velocity in local frame (periapsis is on the +x axis)

		Vector2 position = new Vector2(radius * Mathf.Cos(trueAnomaly), radius * Mathf.Sin(trueAnomaly));

		// Compute velocity by calculating speed using physics
		// then pointing it in the expected direction calculated from differentiating position
		float energyPerUnitMass =
			-ParentGravitationalParameter * (1 - Eccentricity * Eccentricity) / (2 * SemiLatusRectum);
		float speed = Mathf.Sqrt(2 * (energyPerUnitMass + ParentGravitationalParameter / radius));
		// Velocity direction computed as derivative of position w.r.t. true anomaly
		Vector2 velocity = new Vector2(-Mathf.Sin(trueAnomaly), Eccentricity * Mathf.Cos(trueAnomaly)).normalized
		                   * speed;

		// Account for argument of periapsis and convert to global frame
		float cosArg = Mathf.Cos(ArgumentOfPeriapsis);
		float sinArg = Mathf.Sin(ArgumentOfPeriapsis);
		return Tuple.Create(
			new Vector2(cosArg * position.x - sinArg * position.y, sinArg * position.x + cosArg * position.y),
			new Vector2(cosArg * velocity.x - sinArg * velocity.y, sinArg * velocity.x + cosArg * velocity.y)
		);
	}
}
}