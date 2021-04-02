using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Thruster : BlockBehaviour
{
	public float MaxForce;

	private Rigidbody2D _body;
	private VehicleThrusterControl _control;

	private float _forwardBackResponse;
	private float _strafeResponse;
	private float _rotateResponse;

	private void Awake()
	{
		_control = GetComponentInParent<VehicleThrusterControl>();
		_body = GetComponentInParent<Rigidbody2D>();
	}

	private void Start()
	{
		Vector3 localForward = transform.localRotation * Vector3.forward;
		_forwardBackResponse = localForward.y;
		_strafeResponse = localForward.x;
	}

	private void FixedUpdate()
	{
		if (HasPhysics)
		{
			float rawResponse = _forwardBackResponse * _control.ForwardBackCommand +
			                    _strafeResponse * _control.StrafeCommand +
			                    _rotateResponse * _control.RotateCommand;

			_body.AddForceAtPosition(Mathf.Clamp01(rawResponse) * MaxForce * transform.forward, transform.position);
		}
	}
}