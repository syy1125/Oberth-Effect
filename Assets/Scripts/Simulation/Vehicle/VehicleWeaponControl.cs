using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Simulation.Vehicle
{
public class VehicleWeaponControl : MonoBehaviourPun, IWeaponSystemRegistry, IPunObservable
{
	public InputActionReference FireAction;

	private Camera _mainCamera;
	private List<IWeaponSystem> _weapons;
	private Vector2 _aimPoint;

	private void Awake()
	{
		_mainCamera = Camera.main;
		_weapons = new List<IWeaponSystem>();
	}

	private void OnEnable()
	{
		FireAction.action.Enable();
	}

	private void OnDisable()
	{
		FireAction.action.Disable();
	}

	#region Weapon Registry

	public void RegisterBlock(IWeaponSystem block)
	{
		_weapons.Add(block);
	}

	public void UnregisterBlock(IWeaponSystem block)
	{
		bool success = _weapons.Remove(block);
		if (!success)
		{
			Debug.LogError($"Failed to remove weapon system {block}");
		}
	}

	#endregion

	private void FixedUpdate()
	{
		if (photonView.IsMine)
		{
			_aimPoint = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
		}

		foreach (IWeaponSystem weapon in _weapons)
		{
			weapon.SetAimPoint(_aimPoint);
		}

		if (photonView.IsMine)
		{
			bool firing = FireAction.action.ReadValue<float>() > 0.5f;
			foreach (IWeaponSystem weapon in _weapons)
			{
				weapon.SetFiring(firing);
			}
		}
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(_aimPoint);
		}
		else
		{
			_aimPoint = (Vector2) stream.ReceiveNext();
		}
	}
}
}