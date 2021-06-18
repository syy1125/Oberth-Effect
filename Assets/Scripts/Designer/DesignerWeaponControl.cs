using System.Collections.Generic;
using Syy1125.OberthEffect.Blocks.Weapons;
using UnityEngine;

namespace Syy1125.OberthEffect.Designer
{
// In the designer, have the weapons point straight up, in their default aim point.
public class DesignerWeaponControl : MonoBehaviour, IWeaponSystemRegistry
{
	private List<IWeaponSystem> _weapons;

	private void Awake()
	{
		_weapons = new List<IWeaponSystem>();
	}

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

	private void FixedUpdate()
	{
		foreach (IWeaponSystem weapon in _weapons)
		{
			weapon.SetAimPoint(weapon.transform.TransformPoint(Vector3.up));
		}
	}
}
}