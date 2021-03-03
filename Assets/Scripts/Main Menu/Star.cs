using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(TrailRenderer))]
public class Star : MonoBehaviour
{
	public float GravityFactor = 1f;

	private Rigidbody _rigidbody;
	private TrailRenderer _trail;
	private Gradient _baseGradient;
	private float _mass;

	private void Awake()
	{
		_rigidbody = GetComponent<Rigidbody>();
		_trail = GetComponent<TrailRenderer>();
		_baseGradient = _trail.colorGradient;
	}

	public void SetMass(float mass)
	{
		_mass = mass;
		float scaleFactor = Mathf.Sqrt(mass);
		transform.localScale *= scaleFactor;
		_trail.widthMultiplier *= scaleFactor;
	}

	public void SetColor(Color color)
	{
		GetComponent<SpriteRenderer>().color = color;

		GradientColorKey[] colorKeys = _baseGradient.colorKeys
			.Select(key => new GradientColorKey(key.color * color, key.time)).ToArray();
		GradientAlphaKey[] alphaKeys = _baseGradient.alphaKeys
			.Select(key => new GradientAlphaKey(key.alpha * color.a, key.time)).ToArray();
		var gradient = new Gradient();
		gradient.SetKeys(colorKeys, alphaKeys);
		_trail.colorGradient = gradient;
	}

	private void FixedUpdate()
	{
		Vector3 position = transform.position;
		Vector3 acceleration = Vector3.zero;
		var stars = transform.parent.GetComponentsInChildren<Star>();

		foreach (Star star in stars)
		{
			if (star == this) continue;
			Vector3 r = star.transform.position - position;
			acceleration += star._mass / r.sqrMagnitude * r.normalized;
		}

		acceleration *= GravityFactor;

		_rigidbody.AddForce(acceleration, ForceMode.Acceleration);
	}
}