using System;
using System.Threading;
using Syy1125.OberthEffect.Lib.Math;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Prototyping
{
public class InterceptTest : MonoBehaviour
{
	public Transform StartPoint;
	public Transform EndPoint;
	public Transform Target;
	public float MoveTime = 5f;
	private float _timer;

	public Transform Missile;
	public float MissileAcceleration = 2f;
	public Vector2 AccelerationVector;
	public Vector2 MissileVelocity;

	public Text Countdown;

	private void Start()
	{
		_timer = 0f;

		// HalleySolver.FindRoot(new PolynomialExpression(88.45f, -173.4f, 85.16f, 0, -4), 1.15f, epsilon: 1e-3f);
	}

	private void Update()
	{
		_timer += Time.deltaTime;
		
		Vector2 targetPosition = Vector2.Lerp(StartPoint.position, EndPoint.position, _timer / MoveTime)
		                         - (Vector2) Missile.transform.position;
		Vector2 targetVelocity = (Vector2) (EndPoint.position - StartPoint.position) / MoveTime - MissileVelocity;
		
		Target.transform.position = Vector2.Lerp(StartPoint.position, EndPoint.position, _timer / MoveTime);
		
		InterceptSolver.MissileIntercept(
			targetPosition, targetVelocity, MissileAcceleration, out Vector2 accelerationVector, out float hitTime
		);
		
		if (hitTime > 0f)
		{
			AccelerationVector = accelerationVector;
			Countdown.text = hitTime.ToString("F");
		}
		
		MissileVelocity += AccelerationVector * Time.deltaTime;
		Missile.position += (Vector3) MissileVelocity * Time.deltaTime;
		Missile.rotation = Quaternion.LookRotation(Vector3.forward, accelerationVector);
	}

	// public float Speed = 2f;


	// private void OnDrawGizmos()
	// {
	// 	if (Target == null || T1 == null) return;
	//
	// 	Vector2 targetVelocity = T1.position - Target.position;
	// 	bool hit = InterceptSolver.ProjectileIntercept(
	// 		Target.position, targetVelocity, Speed,
	// 		out Vector2 projectileVelocity, out float time
	// 	);
	//
	// 	Gizmos.matrix = Matrix4x4.identity;
	// 	Gizmos.color = Color.yellow;
	// 	Gizmos.DrawWireSphere(Target.position + (Vector3) targetVelocity * time, 0.5f);
	//
	// 	Gizmos.color = hit ? Color.cyan : Color.red;
	// 	Gizmos.DrawWireSphere(projectileVelocity * time, 0.5f);
	// }
}
}