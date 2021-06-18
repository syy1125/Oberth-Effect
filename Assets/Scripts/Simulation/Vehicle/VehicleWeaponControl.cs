using System;
using System.Collections.Generic;
using Syy1125.OberthEffect.Blocks.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Simulation.Vehicle
{
public class VehicleWeaponControl : MonoBehaviour, IWeaponSystemRegistry
{
	private Camera _mainCamera;
	private List<IWeaponSystem> _weapons;

	private void Awake()
	{
		_mainCamera = Camera.main;
		_weapons = new List<IWeaponSystem>();
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
		Vector2 mousePosition = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
		foreach (IWeaponSystem weapon in _weapons)
		{
			weapon.SetAimPoint(mousePosition);
		}
	}
}
}