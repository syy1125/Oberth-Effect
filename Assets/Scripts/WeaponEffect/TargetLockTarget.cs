using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PhotonView))]
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
		return _body.bodyType == RigidbodyType2D.Static ? (Vector2) transform.position : _body.worldCenterOfMass;
	}

	public Vector2 GetEffectiveVelocity()
	{
		return _body.velocity;
	}
}
}