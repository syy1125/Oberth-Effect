using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Foundation.Utils;
using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Simulation.Game;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Simulation.Construct
{
[RequireComponent(typeof(Rigidbody2D))]
public class VehicleWeaponControl : MonoBehaviourPun,
	IWeaponSystemRegistry,
	IPunObservable,
	IVehicleDeathListener,
	IIncomingMissileReceiver
{
	public InputActionReference LookAction;
	public InputActionReference FireAction1;
	public InputActionReference FireAction2;
	public InputActionReference FireAction3;
	public InputActionReference FireAction4;
	public InputActionReference TargetLockAction;

	private Camera _mainCamera;
	private Rigidbody2D _body;
	private Vector2 _localAimPoint;

	private List<IWeaponSystem> _weapons;
	private bool _weaponListChanged;
	public List<Missile> IncomingMissiles { get; private set; }
	public UnityEvent OnIncomingMissileAdded;

	public bool TargetLock { get; private set; }
	public TargetLockTarget CurrentTarget { get; private set; }
	public int? TargetPhotonViewId => CurrentTarget == null ? (int?) null : CurrentTarget.PhotonViewId;

	private float _pdRange;
	private ContactFilter2D _pdFilter;
	private List<Collider2D> _pdHits;

	private void Awake()
	{
		_mainCamera = Camera.main;
		_body = GetComponent<Rigidbody2D>();
		_weapons = new List<IWeaponSystem>();
		IncomingMissiles = new List<Missile>();

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

		while (isActiveAndEnabled)
		{
			bool isMine = photonView.IsMine;
			bool firing1 = FireAction1.action.ReadValue<float>() > 0.5f;
			bool firing2 = FireAction2.action.ReadValue<float>() > 0.5f;
			bool firing3 = FireAction3.action.ReadValue<float>() > 0.5f;
			bool firing4 = FireAction4.action.ReadValue<float>() > 0.5f;

			Vector3 aimPoint = GetAimPoint(isMine);

			if (isMine)
			{
				if (!TargetLock)
				{
					FindTarget(aimPoint);
				}
				else if (CurrentTarget == null || !CurrentTarget.isActiveAndEnabled)
				{
					Debug.Log("WeaponControl target is null or disabled, unlocking target.");
					TargetLock = false;
					FindTarget(aimPoint);
				}

				CleanUpIncomingMissiles();
				List<PointDefenseTargetData> pdTargetData = GetPointDefenseTargets();
				SendWeaponCommands(aimPoint, true, firing1, firing2, firing3, firing4, pdTargetData);
			}
			else
			{
				SendWeaponCommands(aimPoint, false, firing1, firing2, firing3, firing4, null);
			}

			yield return new WaitForFixedUpdate();
		}
	}

	private Vector3 GetAimPoint(bool isMine)
	{
		Vector3 aimPoint;

		if (isMine && LookAction.action.enabled)
		{
			Vector2 screenPoint = LookAction.action.ReadValue<Vector2>();
			if (PlayerControlConfig.Instance.InvertAim)
			{
				screenPoint = new Vector2(Screen.width, Screen.height) - screenPoint;
			}

			aimPoint = _mainCamera.ScreenToWorldPoint(screenPoint);
			_localAimPoint = transform.InverseTransformPoint(aimPoint);
		}
		else
		{
			aimPoint = transform.TransformPoint(_localAimPoint);
		}

		return aimPoint;
	}

	private void FindTarget(Vector2 aimPoint)
	{
		int ownerTeamIndex = PhotonTeamHelper.GetPlayerTeamIndex(photonView.Owner);

		// Idea is similar to projective geometry
		// Suppose that the vehicle is at (0,0,0) and everything else (cursor and targets) is on the z=1 plane
		// Find the target with sight line closest in angle to the vehicle-cursor line
		Vector2 minCorner = _mainCamera.ViewportToWorldPoint(Vector3.zero);
		Vector2 maxCorner = _mainCamera.ViewportToWorldPoint(Vector3.one);

		// Try to make targeting more natural.
		// Limit the angle of cursor-vehicle-target to 45 degrees.
		// This should cover most of the screen when cursor is at center, or half the screen when cursor is near the edge.
		CurrentTarget = null;
		float bestScore = 45f;

		Vector3 normalizedAimPoint = ProjectPoint(aimPoint, minCorner, maxCorner);

		foreach (var target in TargetLockTarget.ActiveTargets)
		{
			if (target.photonView.IsRoomView)
			{
				if (target.GetComponent<RoomViewTeamProvider>().TeamIndex == ownerTeamIndex) continue;
			}
			else
			{
				if (PhotonTeamHelper.GetPlayerTeamIndex(target.photonView.Owner) == ownerTeamIndex) continue;
			}

			Vector3 normalizedPosition = ProjectPoint(target.GetEffectivePosition(), minCorner, maxCorner);
			float score = Vector3.Angle(normalizedPosition, normalizedAimPoint);

			if (score < bestScore)
			{
				CurrentTarget = target;
				bestScore = score;
			}
		}
	}

	private Vector3 ProjectPoint(Vector2 point, Vector2 minCorner, Vector2 maxCorner)
	{
		return new Vector3(
			MathUtils.InverseLerpUnclamped(minCorner.x, maxCorner.x, point.x) * 2 - 1,
			MathUtils.InverseLerpUnclamped(minCorner.y, maxCorner.y, point.y) * 2 - 1,
			1f
		);
	}

	public void AddIncomingMissile(Missile missile)
	{
		if (photonView.IsMine)
		{
			IncomingMissiles.Add(missile);
			OnIncomingMissileAdded?.Invoke();
		}
	}

	public void RemoveIncomingMissile(Missile missile)
	{
		if (photonView.IsMine)
		{
			IncomingMissiles.Remove(missile);
		}
	}

	private void CleanUpIncomingMissiles()
	{
		for (int i = 0; i < IncomingMissiles.Count;)
		{
			if (IncomingMissiles[i] == null || !IncomingMissiles[i].isActiveAndEnabled)
			{
				IncomingMissiles.RemoveAt(i);
			}
			else
			{
				i++;
			}
		}
	}

	private List<PointDefenseTargetData> GetPointDefenseTargets()
	{
		if (_weaponListChanged)
		{
			_pdRange = _weapons
				.Where(weapon => weapon.WeaponBinding == WeaponBindingGroup.PointDefense)
				.Select(weapon => weapon.GetMaxRange())
				.DefaultIfEmpty(0f)
				.Max();
		}

		List<PointDefenseTargetData> pdTargets = new List<PointDefenseTargetData>();

		if (_pdRange > Mathf.Epsilon)
		{
			// If an incoming missiles is a valid point defense target, always consider it for interception
			foreach (Missile missile in IncomingMissiles)
			{
				var target = missile.GetComponent<PointDefenseTarget>();
				var hitTime = missile.GetHitTime();

				if (target != null && hitTime != null)
				{
					pdTargets.Add(
						new PointDefenseTargetData
						{
							Target = target,
							PriorityScore = 1f / hitTime.Value
						}
					);
				}
			}

			// Find other PD targets
			int count = Physics2D.OverlapCircle(transform.position, _pdRange, _pdFilter, _pdHits);
			int ownerTeamIndex = PhotonTeamHelper.GetPlayerTeamIndex(photonView.Owner);
			Vector2 ownPosition = GetComponent<Rigidbody2D>().worldCenterOfMass;
			Vector2 ownVelocity = GetComponent<Rigidbody2D>().velocity;

			foreach (
				var target in _pdHits
					.Take(count)
					.Select(hit => hit.GetComponentInParent<PointDefenseTarget>())
					.Distinct()
			)
			{
				if (target == null || PhotonTeamHelper.GetPlayerTeamIndex(target.OwnerId) == ownerTeamIndex)
				{
					continue;
				}

				Vector2 relativePosition = (Vector2) target.transform.position - ownPosition;
				Vector2 relativeVelocity = target.GetComponent<Rigidbody2D>().velocity - ownVelocity;
				pdTargets.Add(
					new PointDefenseTargetData
					{
						Target = target,
						PriorityScore = Vector2.Dot(relativePosition, -relativeVelocity) / relativePosition.sqrMagnitude
					}
				);
			}
		}

		return pdTargets;
	}

	private void SendWeaponCommands(
		Vector3 aimPoint, bool isMine, bool firing1, bool firing2, bool firing3, bool firing4,
		List<PointDefenseTargetData> pdTargets
	)
	{
		foreach (IWeaponSystem weapon in _weapons)
		{
			switch (weapon.WeaponBinding)
			{
				case WeaponBindingGroup.Manual1:
				case WeaponBindingGroup.Manual2:
				case WeaponBindingGroup.Manual3:
				case WeaponBindingGroup.Manual4:
					weapon.SetAimPoint(aimPoint);
					weapon.SetTargetPhotonId(TargetPhotonViewId);

					if (isMine)
					{
						weapon.SetFiring(
							weapon.WeaponBinding switch
							{
								WeaponBindingGroup.Manual1 => firing1,
								WeaponBindingGroup.Manual2 => firing2,
								WeaponBindingGroup.Manual3 => firing3,
								WeaponBindingGroup.Manual4 => firing4,
								_ => throw new ArgumentException()
							}
						);
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
		else if (TargetPhotonViewId != null)
		{
			Debug.Log($"Locking onto {TargetPhotonViewId.Value}");
			TargetLock = true;
		}
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(_localAimPoint);
			stream.SendNext(TargetPhotonViewId);
		}
		else
		{
			_localAimPoint = (Vector2) stream.ReceiveNext();
			int? targetPhotonViewId = (int?) stream.ReceiveNext();
			CurrentTarget = targetPhotonViewId == null
				? null
				: PhotonView.Find(targetPhotonViewId.Value)?.GetComponent<TargetLockTarget>();
		}
	}
}
}