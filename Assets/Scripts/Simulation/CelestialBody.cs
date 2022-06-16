using System;
using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Lib.Math;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation
{
public class CelestialBody : MonoBehaviourPun, IDamageable
{
	public delegate void OrbitUpdateEvent(Vector2 parentPosition, bool init);

	[Header("Characteristics")]
	public float GravitationalParameter;

	[Header("Orbit")]
	public CelestialBody ParentBody;
	public float SemiLatusRectum;
	public float Eccentricity;
	public float ArgumentOfPeriapsis;
	public float TrueAnomalyAtEpoch;

	[Header("Config")]
	public Color RadarColor;
	public float RadarSize;

	public static List<CelestialBody> CelestialBodies = new();

	private Orbit2D _orbit;

	public Rigidbody2D Body { get; private set; }
	public event OrbitUpdateEvent OnOrbitUpdate;

	private void Awake()
	{
		Body = gameObject.GetComponent<Rigidbody2D>();
		var circleCollider = gameObject.AddComponent<CircleCollider2D>();
		var pointEffector = gameObject.AddComponent<PointEffector2D>();

		circleCollider.radius = Mathf.Sqrt(GravitationalParameter) / 0.0025f;
		circleCollider.isTrigger = true;
		circleCollider.usedByEffector = true;

		pointEffector.forceMagnitude = -GravitationalParameter;
		pointEffector.forceMode = EffectorForceMode2D.InverseSquared;
	}

	private void OnEnable()
	{
		CelestialBodies.Add(this);

		if (ParentBody != null)
		{
			ParentBody.OnOrbitUpdate += UpdateOrbit;

			_orbit = new Orbit2D
			{
				ParentGravitationalParameter = ParentBody.GravitationalParameter,
				SemiLatusRectum = SemiLatusRectum,
				Eccentricity = Eccentricity,
				ArgumentOfPeriapsis = ArgumentOfPeriapsis * Mathf.Deg2Rad,
				TrueAnomalyAtEpoch = TrueAnomalyAtEpoch * Mathf.Deg2Rad
			};
		}
	}

	private void Start()
	{
		// To ensure that the correct coordinates are propagated, only the top node of a celestial body hierarchy runs on unity update.
		// Other celestial bodies update through recursive event invocations.
		if (ParentBody == null)
		{
			OnOrbitUpdate?.Invoke(Vector2.zero, true);
		}
	}

	private void OnDisable()
	{
		CelestialBodies.Remove(this);

		if (ParentBody != null)
		{
			ParentBody.OnOrbitUpdate -= UpdateOrbit;
		}
	}

	private void FixedUpdate()
	{
		// To ensure that the correct coordinates are propagated, only the top node of a celestial body hierarchy runs on unity update.
		// Other celestial bodies update through recursive event invocations.
		if (_orbit == null)
		{
			OnOrbitUpdate?.Invoke(Vector2.zero, false);
		}
	}

	private void UpdateOrbit(Vector2 parentPosition, bool init)
	{
		(Vector2 localPosition, Vector2 _) = _orbit.GetStateVectorAt(SynchronizedTimer.Instance.SynchronizedTime);
		Vector2 position = parentPosition + localPosition;

		if (init)
		{
			transform.position = position;
		}
		else
		{
			Body.MovePosition(position);
		}

		OnOrbitUpdate?.Invoke(position, init);
	}

	public Vector2 GetEffectiveVelocity(float time)
	{
		if (_orbit == null) return Vector2.zero;
		return ParentBody.GetEffectiveVelocity(time) + _orbit.GetStateVectorAt(time).Item2;
	}

	#region Damageable

	public bool IsMine => true;
	public int OwnerId => photonView.OwnerActorNr;

	public Tuple<Vector2, Vector2> GetExplosionDamageBounds()
	{
		return new Tuple<Vector2, Vector2>(Vector2.zero, Vector2.zero);
	}

	public int GetExplosionGridResolution()
	{
		return 1;
	}

	public Predicate<Vector2> GetPointInBoundPredicate()
	{
		return _ => false;
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

	#endregion
}
}