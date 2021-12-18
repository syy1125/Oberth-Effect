using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks.Weapons;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Simulation.Construct
{
public class VehicleWeaponControl : MonoBehaviourPun, IWeaponSystemRegistry, IPunObservable, IVehicleDeathListener
{
	public InputActionReference LookAction;
	public InputActionReference FireAction1;
	public InputActionReference FireAction2;

	private Camera _mainCamera;
	private Vector2 _localAimPoint;

	private List<IWeaponSystem> _weapons;
	private bool _weaponListChanged;

	private float _pdRange;
	private ContactFilter2D _pdFilter;
	private List<Collider2D> _pdHits;

	private void Awake()
	{
		_mainCamera = Camera.main;
		_weapons = new List<IWeaponSystem>();

		_pdFilter = new ContactFilter2D
		{
			layerMask = 1 << LayerConstants.WEAPON_PROJECTILE_LAYER,
			useLayerMask = true,
			useTriggers = true
		};
		_pdHits = new List<Collider2D>();
	}

	private void Start()
	{
		StartCoroutine(LateFixedUpdate());
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
		_weaponListChanged = true;
	}

	public void UnregisterBlock(IWeaponSystem block)
	{
		bool success = _weapons.Remove(block);
		if (success)
		{
			_weaponListChanged = true;
		}
		else
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

			Vector3 aimPoint;
			if (isMine && LookAction.action.enabled)
			{
				aimPoint = _mainCamera.ScreenToWorldPoint(LookAction.action.ReadValue<Vector2>());
				_localAimPoint = transform.InverseTransformPoint(aimPoint);
			}
			else
			{
				aimPoint = transform.TransformPoint(_localAimPoint);
			}

			if (_weaponListChanged)
			{
				_pdRange = _weapons
					.Where(weapon => weapon.WeaponBinding == WeaponBindingGroup.PointDefense)
					.Select(weapon => weapon.GetMaxRange())
					.DefaultIfEmpty(0f)
					.Max();
			}

			List<PointDefenseTarget> pdTargets = new List<PointDefenseTarget>();
			if (_pdRange > Mathf.Epsilon)
			{
				int count = Physics2D.OverlapCircle(transform.position, _pdRange, _pdFilter, _pdHits);
				pdTargets = _pdHits
					.Take(count)
					.Select(hit => hit.GetComponentInParent<PointDefenseTarget>())
					.Where(target => target != null && target.OwnerId != photonView.OwnerActorNr)
					.ToList();
			}

			foreach (IWeaponSystem weapon in _weapons)
			{
				switch (weapon.WeaponBinding)
				{
					case WeaponBindingGroup.Manual1:
						weapon.SetAimPoint(aimPoint);
						if (isMine)
						{
							weapon.SetFiring(firing1);
						}

						break;
					case WeaponBindingGroup.Manual2:
						weapon.SetAimPoint(aimPoint);
						if (isMine)
						{
							weapon.SetFiring(firing2);
						}

						break;
					case WeaponBindingGroup.PointDefense:
						if (isMine)
						{
							weapon.SetPointDefenseTargetList(pdTargets);
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
			stream.SendNext(_localAimPoint);
		}
		else
		{
			_localAimPoint = (Vector2) stream.ReceiveNext();
		}
	}
}
}