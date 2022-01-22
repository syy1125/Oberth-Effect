using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation
{
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(ITargetNameProvider))]
public class TargetLockTarget : MonoBehaviourPun
{
	public static readonly List<TargetLockTarget> ActiveTargets = new List<TargetLockTarget>();

	private Rigidbody2D _body;

	private void Awake()
	{
		_body = GetComponent<Rigidbody2D>();
	}

	private void OnEnable()
	{
		ActiveTargets.Add(this);
	}

	private void OnDisable()
	{
		ActiveTargets.Remove(this);
	}

	public Vector2 GetEffectivePosition()
	{
		return _body == null ? (Vector2) transform.position : _body.worldCenterOfMass;
	}
}
}