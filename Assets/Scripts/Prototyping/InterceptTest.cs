using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Prototyping
{
public class InterceptTest : MonoBehaviour
{
	public Transform Target;
	public InputActionReference MoveAction;
	public Vector2 TargetPosition;
	public Vector2 TargetVelocity;
	public float TargetAcceleration = 2f;

	public Transform Missile;
	public float MissileAcceleration = 2f;
	public Vector2 MissilePosition;
	public Vector2 MissileVelocity;

	public Text Countdown;

	private void Start()
	{
		MoveAction.action.Enable();

		TargetPosition = Target.position;
		MissilePosition = Missile.position;
	}

	private void Update()
	{
		Vector2 input = MoveAction.action.ReadValue<Vector2>();
		if (input.sqrMagnitude > 1f) input.Normalize();

		TargetVelocity += input * Time.deltaTime;
		TargetPosition += TargetVelocity * Time.deltaTime;
		Target.position = TargetPosition;

		bool converged = InterceptSolver.MissileIntercept(
			TargetPosition - MissilePosition, TargetVelocity - MissileVelocity, MissileAcceleration,
			out Vector2 accelerationVector, out float hitTime
		);

		if (!converged)
		{
			Debug.LogWarning("Failed to converge");
		}

		MissileVelocity += accelerationVector * Time.deltaTime;
		MissilePosition += MissileVelocity * Time.deltaTime;

		Missile.position = MissilePosition;
		Missile.rotation = Quaternion.LookRotation(Vector3.forward, accelerationVector);

		Countdown.text = hitTime.ToString("F2");
	}
}
}