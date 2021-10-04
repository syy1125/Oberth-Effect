using System;
using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation
{
public class CelestialBody : MonoBehaviourPun, IDamageable
{
	[Header("Characteristics")]
	public float GravitationalParameter;

	[Header("Orbit")]
	public CelestialBody ParentBody;
	public float Eccentricity;
	public float SemiLatusRectum;
	public float ArgumentOfPeriapsis;
	public float TrueAnomalyAtEpoch;
	private List<CelestialBody> _children = new List<CelestialBody>();

	private void OnEnable()
	{
		if (ParentBody != null)
		{
			ParentBody._children.Add(this);
		}
	}

	private void Start()
	{
		var circleCollider = gameObject.AddComponent<CircleCollider2D>();
		var pointEffector = gameObject.AddComponent<PointEffector2D>();

		circleCollider.radius = Mathf.Sqrt(GravitationalParameter) / 0.001f;
		circleCollider.isTrigger = true;
		circleCollider.usedByEffector = true;

		pointEffector.forceMagnitude = -GravitationalParameter;
		pointEffector.forceMode = EffectorForceMode2D.InverseSquared;
	}

	private void OnDisable()
	{
		if (ParentBody != null)
		{
			bool success = ParentBody._children.Remove(this);
			if (!success)
			{
				Debug.LogError(
					$"Failed to remove CelestialBody {gameObject} from parent {ParentBody}'s list of children"
				);
			}
		}
	}

	private void Update()
	{
		// To ensure that the correct coordinates are propagated, only the top node of a celestial body hierarchy runs on unity update.
		// Other celestial bodies update through recursive function calls.
		if (ParentBody == null)
		{
			UpdateOrbit();
		}
	}

	private void UpdateOrbit()
	{
		if (ParentBody != null)
		{
			float trueAnomaly = GetTrueAnomalyAt(Time.timeSinceLevelLoad);
			float radius = GetRadiusAt(trueAnomaly);
			float angle = trueAnomaly + ArgumentOfPeriapsis * Mathf.Deg2Rad;

			Vector3 position = ParentBody.transform.position;
			position.x += radius * Mathf.Cos(angle);
			position.y += radius * Mathf.Sin(angle);
			transform.position = position;
		}

		foreach (CelestialBody child in _children)
		{
			child.UpdateOrbit();
		}
	}

	#region Orbital Mechanics

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
			ParentBody.GravitationalParameter
			* Mathf.Pow(Mathf.Abs(1 - Mathf.Pow(Eccentricity, 2)) / SemiLatusRectum, 3)
		);

		if (Mathf.Approximately(Eccentricity, 1f)) // Parabolic case
		{
			// Reference: https://en.wikipedia.org/wiki/Parabolic_trajectory#Barker's_equation

			// Solve for time of periapsis passage
			float parabolicAnomalyAtEpoch = Mathf.Tan(TrueAnomalyAtEpoch / 2);
			float periapsisTime = -Mathf.Sqrt(Mathf.Pow(SemiLatusRectum, 3) / ParentBody.GravitationalParameter)
			                      / 2
			                      * (parabolicAnomalyAtEpoch + Mathf.Pow(parabolicAnomalyAtEpoch, 3) / 2);

			// Use the substitutions given in the reference
			float a = 3f
			          * Mathf.Sqrt(ParentBody.GravitationalParameter / Mathf.Pow(SemiLatusRectum, 3))
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
				Eccentricity * (float) Math.Sinh(hyperbolicAnomalyAtEpoch) - hyperbolicAnomalyAtEpoch;

			float meanAnomaly = meanAnomalyAtEpoch + meanMotion * time;

			// Use Newton's solver
			// Initial value estimate provided by Danby, Fundamentals of Celesital Mechanics (p.176)
			float hyperbolicAnomaly = Mathf.Log(2 * meanAnomaly / Eccentricity + 1.8f);
			for (var i = 0; i < SOLVER_ITERATION_LIMIT; i++)
			{
				float dH = -(Eccentricity * (float) Math.Sinh(hyperbolicAnomaly) - hyperbolicAnomaly - meanAnomaly)
				           / (Eccentricity * (float) Math.Cosh(hyperbolicAnomaly) - 1);
				hyperbolicAnomaly += dH;
				if (Mathf.Abs(dH) < SOLVER_SMALL_THRESHOLD) break;
			}

			return 2
			       * Mathf.Atan2(
				       Mathf.Sqrt(Eccentricity + 1) * (float) Math.Sinh(hyperbolicAnomaly / 2),
				       Mathf.Sqrt(Eccentricity - 1) * (float) Math.Cosh(hyperbolicAnomaly / 2)
			       );
		}
	}

	private float GetRadiusAt(float trueAnomaly)
	{
		return SemiLatusRectum / (1 + Eccentricity * Mathf.Cos(trueAnomaly));
	}

	#endregion

	public bool IsMine => true;
	public int OwnerId => photonView.OwnerActorNr;

	public Tuple<Vector2, Vector2> GetExplosionDamageBounds()
	{
		return new Tuple<Vector2, Vector2>(Vector2.zero, Vector2.zero);
	}

	public void TakeDamage(
		DamageType damageType, ref float damage, float armorPierce, out bool damageExhausted
	)
	{
		damageExhausted = true;
	}

	public void RequestBeamDamage(
		DamageType damageType, float damage, float armorPierce, int ownerId, Vector2 beamStart, Vector2 beamEnd
	)
	{}
}
}