using System.Collections;
using Syy1125.OberthEffect.Foundation.Physics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Prototyping
{
[RequireComponent(typeof(Rigidbody2D))]
public class ColliderTest : MonoBehaviour
{
	public bool EstimateDistance;
	public Vector2 TargetVelocity;

	private void Start()
	{
		if (EstimateDistance)
		{
			StartCoroutine(LateFixedUpdate());
		}
	}

	private void Update()
	{
		// if (Keyboard.current.spaceKey.wasPressedThisFrame)
		// {
		// 	GetComponent<Rigidbody2D>().velocity = TargetVelocity;
		// }
	}

	private void FixedUpdate()
	{
		var body = GetComponent<Rigidbody2D>();

		if (Keyboard.current.spaceKey.isPressed)
		{
			body.AddTorque(1000 * body.inertia * Mathf.Deg2Rad);
		}

		Debug.Log(body.rotation);
	}

	private IEnumerator LateFixedUpdate()
	{
		Vector2 prevPosition = transform.position;
		yield return new WaitForFixedUpdate();

		while (isActiveAndEnabled)
		{
			foreach (var referenceFrame in ReferenceFrameProvider.ReferenceFrames)
			{
				Debug.Log(
					$"Estimated min distance to {referenceFrame.name} is {referenceFrame.GetMinApproachSqrDistance(prevPosition, transform.position)}"
				);
			}

			prevPosition = transform.position;
			yield return new WaitForFixedUpdate();
		}
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		Debug.Log($"Collision with {other.name}");
		Debug.Log($"Trigger position {transform.position}");
		Debug.Log($"Other position {other.transform.position}");

		var referenceFrame = other.GetComponent<ReferenceFrameProvider>();
		if (referenceFrame != null)
		{
			Debug.Log($"{other.name} prev position {referenceFrame.PrevPosition}");
		}
	}
}
}