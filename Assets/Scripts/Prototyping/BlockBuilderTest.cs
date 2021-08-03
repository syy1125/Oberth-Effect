using System.Collections.Generic;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.Blocks.Weapons;
using Syy1125.OberthEffect.Spec;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Prototyping
{
public class BlockBuilderTest : MonoBehaviour, IWeaponSystemRegistry
{
	public string BlockId;

	public Transform Parent;
	public Vector2Int RootPosition;
	public int Rotation;

	private List<IWeaponSystem> _weapons;

	private void Awake()
	{
		ModLoader.Init();
		ModLoader.LoadModList();
		ModLoader.LoadAllEnabledContent();

		_weapons = new List<IWeaponSystem>();
	}

	private void Start()
	{
		GameObject blockObject = BlockBuilder.BuildFromSpec(
			BlockDatabase.Instance.GetSpecInstance(BlockId).Spec, Parent, RootPosition, Rotation
		);
	}

	private void FixedUpdate()
	{
		Vector2 aimPoint = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
		bool firing = Mouse.current.leftButton.isPressed;

		foreach (IWeaponSystem weapon in _weapons)
		{
			(weapon as IResourceConsumerBlock)?.SatisfyResourceRequestAtLevel(1f);
			weapon.SetAimPoint(aimPoint);
			weapon.SetFiring(firing);
		}
	}

	public void RegisterBlock(IWeaponSystem block)
	{
		_weapons.Add(block);
	}

	public void UnregisterBlock(IWeaponSystem block)
	{
		_weapons.Remove(block);
	}
}
}