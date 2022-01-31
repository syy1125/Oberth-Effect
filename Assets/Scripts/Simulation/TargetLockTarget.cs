using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Simulation.Construct;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation
{
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PhotonView))]
public class TargetLockTarget : MonoBehaviourPun, IGuidedWeaponTarget, IVehicleDeathListener
{
	public static readonly List<TargetLockTarget> ActiveTargets = new List<TargetLockTarget>();

	public int PhotonViewId { get; private set; }

	private Rigidbody2D _body;

	private void Awake()
	{
		PhotonViewId = GetComponent<PhotonView>().ViewID;
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

	public void OnVehicleDeath()
	{
		enabled = false;
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