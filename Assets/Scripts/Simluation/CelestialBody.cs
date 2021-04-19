using System.Collections.Generic;
using UnityEngine;

namespace Syy1125.OberthEffect.Simluation
{
	[RequireComponent(typeof(CircleCollider2D))]
	public class CelestialBody : MonoBehaviour
	{
		public float Mass;

		private List<Rigidbody2D> _affected;

		private void Awake()
		{
			_affected = new List<Rigidbody2D>();
		}

		private void Start()
		{
			float radiusLimit = Mathf.Sqrt(Mass / 0.001f);
			GetComponent<CircleCollider2D>().radius = radiusLimit;
		}

		private void OnTriggerEnter2D(Collider2D other)
		{
			Rigidbody2D otherBody = other.attachedRigidbody;
			if (otherBody != null)
			{
				_affected.Add(otherBody);
			}
		}

		private void OnTriggerExit2D(Collider2D other)
		{
			Rigidbody2D otherBody = other.attachedRigidbody;
			if (otherBody != null)
			{
				_affected.Remove(otherBody);
			}
		}

		private void FixedUpdate()
		{
			Vector2 center = transform.position;

			foreach (Rigidbody2D body in _affected)
			{
				Vector2 r = body.worldCenterOfMass - center;
				// Avoid potential division by zero issue. Note that a black hole has event horizon radius 2M so this bound should line up.
				float strength = Mass / Mathf.Max(r.sqrMagnitude, 0.5f);
				body.AddForce(-r.normalized * strength);
			}
		}
	}
}