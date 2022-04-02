using System;
using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Lib.Math;
using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;
using UnityEngine.UIElements;

namespace Syy1125.OberthEffect.Simulation
{
public class CelestialBody : MonoBehaviourPun, IDamageable
{
	public delegate void OrbitUpdateEvent(bool init);

	[Header("Characteristics")]
	public float GravitationalParameter;

	[Header("Orbit")]
	public CelestialBody ParentBody;
	public float SemiLatusRectum;
	public float Eccentricity;
	public float ArgumentOfPeriapsis;
	public float TrueAnomalyAtEpoch;

	private Orbit2D _orbit;
	private float _referenceTime;

	public Rigidbody2D Body { get; private set; }
	public OrbitUpdateEvent OnOrbitUpdate;

	private void Awake()
	{
		Body = gameObject.AddComponent<Rigidbody2D>();
		var circleCollider = gameObject.AddComponent<CircleCollider2D>();
		var pointEffector = gameObject.AddComponent<PointEffector2D>();

		Body.isKinematic = true;

		circleCollider.radius = Mathf.Sqrt(GravitationalParameter) / 0.0025f;
		circleCollider.isTrigger = true;
		circleCollider.usedByEffector = true;

		pointEffector.forceMagnitude = -GravitationalParameter;
		pointEffector.forceMode = EffectorForceMode2D.InverseSquared;
	}

	private void OnEnable()
	{
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
			_referenceTime = (float) PhotonNetwork.Time;
		}
	}

	private void Start()
	{
		// To ensure that the correct coordinates are propagated, only the top node of a celestial body hierarchy runs on unity update.
		// Other celestial bodies update through recursive event invocations.
		if (ParentBody == null)
		{
			OnOrbitUpdate?.Invoke(true);
		}
	}

	private void OnDisable()
	{
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
			OnOrbitUpdate?.Invoke(false);
		}
	}

	private void UpdateOrbit(bool init)
	{
		(Vector2 position, Vector2 _) = _orbit.GetStateVectorAt((float) PhotonNetwork.Time - _referenceTime);

		if (init)
		{
			transform.position = ParentBody.transform.position + (Vector3) position;
		}
		else
		{
			Body.MovePosition(ParentBody.Body.position + position);
		}

		OnOrbitUpdate?.Invoke(init);
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