using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks.Weapons;
using Syy1125.OberthEffect.Common.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Simulation.Vehicle
{
public class VehicleWeaponControl : MonoBehaviourPun, IWeaponSystemRegistry, IPunObservable
{
	public InputActionReference FireAction1;
	public InputActionReference FireAction2;

	private Camera _mainCamera;
	private List<IWeaponSystem> _weapons;
	private Vector2 _mouseAimPoint;

	private void Awake()
	{
		_mainCamera = Camera.main;
		_weapons = new List<IWeaponSystem>();
	}

	private void OnEnable()
	{
		FireAction1.action.Enable();
		FireAction2.action.Enable();
	}

	private void Start()
	{
		StartCoroutine(LateFixedUpdate());
	}

	private void OnDisable()
	{
		FireAction1.action.Disable();
		FireAction2.action.Disable();
	}

	public void OnVehicleDeath()
	{
		foreach (IWeaponSystem weapon in _weapons)
		{
			weapon.SetFiring(false);
		}

		enabled = false;
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

	private IEnumerator LateFixedUpdate()
	{
		yield return new WaitForFixedUpdate();

		while (enabled)
		{
			bool isMine = photonView.IsMine;
			bool firing1 = FireAction1.action.ReadValue<float>() > 0.5f;
			bool firing2 = FireAction2.action.ReadValue<float>() > 0.5f;

			if (isMine)
			{
				_mouseAimPoint = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
			}

			foreach (IWeaponSystem weapon in _weapons)
			{
				switch (weapon.WeaponBinding)
				{
					case WeaponBindingGroup.Manual1:
						weapon.SetAimPoint(_mouseAimPoint);
						if (isMine)
						{
							weapon.SetFiring(firing1);
						}

						break;
					case WeaponBindingGroup.Manual2:
						weapon.SetAimPoint(_mouseAimPoint);
						if (isMine)
						{
							weapon.SetFiring(firing2);
						}

						break;
				}
			}

			yield return new WaitForFixedUpdate();
		}
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(_mouseAimPoint);
		}
		else
		{
			_mouseAimPoint = (Vector2) stream.ReceiveNext();
		}
	}
}
}