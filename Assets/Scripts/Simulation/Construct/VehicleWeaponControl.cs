using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks.Weapons;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Simulation.Construct
{
[RequireComponent(typeof(Rigidbody2D))]
public class VehicleWeaponControl : MonoBehaviourPun, IWeaponSystemRegistry, IPunObservable, IVehicleDeathListener
{
	public InputActionReference LookAction;
	public InputActionReference FireAction1;
	public InputActionReference FireAction2;
	public InputActionReference TargetLockAction;

	private Camera _mainCamera;
	private Rigidbody2D _body;
	private Vector2 _localAimPoint;

	private List<IWeaponSystem> _weapons;
	private bool _weaponListChanged;

	public bool TargetLock { get; private set; }
	public int? TargetPhotonId { get; private set; }

	private float _pdRange;
	private ContactFilter2D _pdFilter;
	private List<Collider2D> _pdHits;

	private void Awake()
	{
		_mainCamera = Camera.main;
		_body = GetComponent<Rigidbody2D>();
		_weapons = new List<IWeaponSystem>();

		_pdFilter = new ContactFilter2D
		{
			layerMask = 1 << LayerConstants.WEAPON_PROJECTILE_LAYER,
			useLayerMask = true,
			useTriggers = true
		};
		_pdHits = new List<Collider2D>();
	}

	private void OnEnable()
	{
		TargetLockAction.action.performed += ToggleTargetLock;
	}

	private void Start()
	{
		StartCoroutine(LateFixedUpdate());
	}

	private void OnDisable()
	{
		TargetLockAction.action.performed -= ToggleTargetLock;
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

			Vector3 aimPoint = GetAimPoint(isMine);

			if (isMine)
			{
				FindTarget(aimPoint);
			}

			List<PointDefenseTarget> pdTargets = GetPointDefenseTargets(isMine);
			SendWeaponCommands(aimPoint, isMine, firing1, firing2, pdTargets);

			yield return new WaitForFixedUpdate();
		}
	}

	private Vector3 GetAimPoint(bool isMine)
	{
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

		return aimPoint;
	}

	private void FindTarget(Vector2 cursorPosition)
	{
		if (TargetLock) return;

		TargetPhotonId = null;
		float minDistance = float.PositiveInfinity;
		int ownerTeamIndex = PhotonTeamHelper.GetPlayerTeamIndex(photonView.Owner);

		foreach (VehicleCore vehicle in VehicleCore.ActiveVehicles)
		{
			if (!vehicle.enabled || vehicle.IsDead) continue;
			if (PhotonTeamHelper.GetPlayerTeamIndex(vehicle.photonView.Owner) == ownerTeamIndex) continue;

			float distance = Vector2.Distance(cursorPosition, vehicle.GetComponent<Rigidbody2D>().worldCenterOfMass);
			if (TargetPhotonId == null || distance < minDistance)
			{
				TargetPhotonId = vehicle.photonView.ViewID;
				minDistance = distance;
			}
		}

		if (TargetPhotonId != null)
		{
			// Try to make targeting more natural. When the cursor is far from the detected target,
			// only set target if the ratio of target-cursor distance to cursor-vehicle distance is smaller than ratio of vehicle-cursor distance to cursor-edge distance.
			Vector2 minCorner = _mainCamera.ViewportToWorldPoint(Vector3.zero);
			Vector2 maxCorner = _mainCamera.ViewportToWorldPoint(Vector3.one);
			Vector2 com = _body.worldCenterOfMass;

			float xRatio = cursorPosition.x > com.x
				? (cursorPosition.x - com.x) / (maxCorner.x - cursorPosition.x)
				: (com.x - cursorPosition.x) / (cursorPosition.x - minCorner.x);
			float yRatio = cursorPosition.y > com.y
				? (cursorPosition.y - com.y) / (maxCorner.y - cursorPosition.y)
				: (com.y - cursorPosition.y) / (cursorPosition.y - minCorner.y);

			float halfScreenSize = Mathf.Min(maxCorner.x - minCorner.x, maxCorner.y - minCorner.y) / 2;
			float threshold = Mathf.Max(
				halfScreenSize, Vector2.Distance(cursorPosition, com) * Mathf.Max(xRatio, yRatio)
			);

			if (minDistance > threshold)
			{
				TargetPhotonId = null;
			}
		}
	}

	private List<PointDefenseTarget> GetPointDefenseTargets(bool isMine)
	{
		if (!isMine) return null;

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
			int ownerTeamIndex = PhotonTeamHelper.GetPlayerTeamIndex(photonView.Owner);
			pdTargets = _pdHits
				.Take(count)
				.Select(hit => hit.GetComponentInParent<PointDefenseTarget>())
				.Where(
					target => target != null
					          && PhotonTeamHelper.GetPlayerTeamIndex(target.OwnerId) != ownerTeamIndex
				)
				.ToList();
		}

		return pdTargets;
	}

	private void SendWeaponCommands(
		Vector3 aimPoint, bool isMine, bool firing1, bool firing2, List<PointDefenseTarget> pdTargets
	)
	{
		foreach (IWeaponSystem weapon in _weapons)
		{
			switch (weapon.WeaponBinding)
			{
				case WeaponBindingGroup.Manual1:
					weapon.SetAimPoint(aimPoint);
					weapon.SetTargetPhotonId(TargetPhotonId);

					if (isMine)
					{
						weapon.SetFiring(firing1);
					}

					break;
				case WeaponBindingGroup.Manual2:
					weapon.SetAimPoint(aimPoint);
					weapon.SetTargetPhotonId(TargetPhotonId);

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
	}

	private void ToggleTargetLock(InputAction.CallbackContext context)
	{
		if (!photonView.IsMine) return;

		if (TargetLock)
		{
			Debug.Log("Turning off target lock");
			TargetLock = false;
		}
		else if (TargetPhotonId != null)
		{
			Debug.Log($"Locking onto {TargetPhotonId.Value}");
			TargetLock = true;
		}
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(_localAimPoint);
			stream.SendNext(TargetPhotonId);
		}
		else
		{
			_localAimPoint = (Vector2) stream.ReceiveNext();
			TargetPhotonId = (int?) stream.ReceiveNext();
		}
	}
}
}