using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Simulation.Construct;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation
{
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(ITargetLockInfoProvider))]
public class TargetLockTarget : MonoBehaviourPun, IGuidedWeaponTarget, IVehicleDeathListener
{
	public static readonly List<TargetLockTarget> ActiveTargets = new List<TargetLockTarget>();

	public int PhotonViewId { get; private set; }

	private ITargetLockInfoProvider _infoProvider;

	private void Awake()
	{
		PhotonViewId = photonView.ViewID;
		_infoProvider = GetComponent<ITargetLockInfoProvider>();
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
		return _infoProvider.GetPosition();
	}

	public Vector2 GetEffectiveVelocity()
	{
		return _infoProvider.GetVelocity();
	}
}
}