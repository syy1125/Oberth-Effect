﻿using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
	public Transform Target;
	public bool FollowCenterOfMass;

	public float Response;
	public float DerivativeTime;
	public float IntegralTime;

	private LinkedList<Vector2> _offsetHistory;
	private Vector2 _velocity;
	private Vector2 _integral;

	private void Awake()
	{
		_offsetHistory = new LinkedList<Vector2>();
		_velocity = Vector2.zero;
		_integral = Vector2.zero;
	}

	private void FixedUpdate()
	{
		var body = Target.GetComponent<Rigidbody2D>();

		var currentPosition = new Vector2(transform.position.x, transform.position.y);
		Vector2 targetPosition = FollowCenterOfMass && body != null
			? body.worldCenterOfMass
			: new Vector2(Target.position.x, Target.position.y);
		Vector2 offset = targetPosition - currentPosition;

		Vector2 derivative = _offsetHistory.Count > 0
			? (offset - _offsetHistory.Last.Value) / Time.fixedDeltaTime
			: Vector2.zero;

		_offsetHistory.AddLast(offset);
		_integral += offset;
		while (_offsetHistory.Count > 1 && _offsetHistory.Count > IntegralTime / Time.fixedDeltaTime)
		{
			_integral -= _offsetHistory.First.Value;
			_offsetHistory.RemoveFirst();
		}

		Vector2 timeScaledIntegral = _integral * Time.fixedDeltaTime;

		_velocity += Response
		             * Time.fixedDeltaTime
		             * (
			             offset
			             + derivative * DerivativeTime
			             + (Mathf.Abs(IntegralTime) < Mathf.Epsilon
				             ? Vector2.zero
				             : timeScaledIntegral / IntegralTime)
		             );

		transform.position += new Vector3(_velocity.x, _velocity.y) * Time.fixedDeltaTime;
	}
}