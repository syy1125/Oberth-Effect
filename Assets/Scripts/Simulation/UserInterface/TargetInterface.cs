using System;
using Photon.Pun;
using Syy1125.OberthEffect.Foundation.Utils;
using Syy1125.OberthEffect.Simulation.Construct;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
[RequireComponent(typeof(CanvasGroup))]
public class TargetInterface : MonoBehaviour
{
	[NonSerialized]
	public VehicleWeaponControl WeaponControl;

	[Header("References")]
	public CameraFollow CameraFollow;
	public Image Frame;
	public Text TargetNameText;
	public Text TargetInfoText;
	public Transform RelativeVelocityDirection;
	public HighlightTarget TargetLockHighlight;

	[Header("Config")]
	public Color TargetColor = new Color(0.75f, 0f, 0f);
	public Color TargetLockColor = Color.red;

	private CanvasGroup _group;
	private int? _targetId;
	private bool _targetLock;
	private GameObject _target;

	private void Awake()
	{
		_group = GetComponent<CanvasGroup>();
	}

	private void Start()
	{
		UpdateTarget(null, false);
	}

	private void Update()
	{
		if (WeaponControl == null || !WeaponControl.isActiveAndEnabled)
		{
			UpdateTarget(null, false);
			return;
		}

		if (WeaponControl.TargetPhotonViewId != _targetId || WeaponControl.TargetLock != _targetLock)
		{
			UpdateTarget(WeaponControl.TargetPhotonViewId, WeaponControl.TargetLock);
		}

		if (_target != null)
		{
			string targetName = _target.GetComponentInParent<ITargetLockInfoProvider>()?.GetName() ?? string.Empty;
			float distance = Vector2.Distance(
				WeaponControl.GetComponent<Rigidbody2D>().worldCenterOfMass,
				_target.GetComponent<IGuidedWeaponTarget>().GetEffectivePosition()
			);
			Vector2 relativeVelocity = _target.GetComponent<IGuidedWeaponTarget>().GetEffectiveVelocity()
			                           - WeaponControl.GetComponent<Rigidbody2D>().velocity;

			TargetNameText.text = WeaponControl.TargetLock ? $"Target locked: {targetName}" : $"Target: {targetName}";
			TargetInfoText.text =
				$"Distance: {PhysicsUnitUtils.FormatDistance(distance)}, RVel: {PhysicsUnitUtils.FormatSpeed(relativeVelocity.magnitude)}";
			RelativeVelocityDirection.rotation = Quaternion.LookRotation(Vector3.forward, relativeVelocity);
		}
	}

	private void UpdateTarget(int? targetId, bool targetLock)
	{
		_targetId = targetId;
		_targetLock = targetLock;
		if (_targetId == null)
		{
			HideTargetingInterface();
			return;
		}

		_target = PhotonView.Find(_targetId.Value).gameObject;
		if (_target == null)
		{
			HideTargetingInterface();
			return;
		}

		CameraFollow.Target = _target.transform;
		_group.alpha = 1f;
		_group.interactable = true;
		_group.blocksRaycasts = true;

		TargetLockHighlight.Target = _target.transform;
		TargetLockHighlight.gameObject.SetActive(true);

		if (WeaponControl.TargetLock)
		{
			TargetInfoText.fontStyle = FontStyle.Bold;
			TargetNameText.fontStyle = FontStyle.Bold;

			foreach (Transform child in transform)
			{
				foreach (Graphic graphic in child.GetComponentsInChildren<Graphic>())
				{
					graphic.color = TargetLockColor;
				}
			}

			foreach (Image image in TargetLockHighlight.GetComponentsInChildren<Image>())
			{
				image.color = TargetLockColor;
				image.pixelsPerUnitMultiplier = 0.5f;
			}
		}
		else
		{
			TargetNameText.fontStyle = FontStyle.Normal;
			TargetInfoText.fontStyle = FontStyle.Normal;

			foreach (Transform child in transform)
			{
				foreach (Graphic graphic in child.GetComponentsInChildren<Graphic>())
				{
					graphic.color = TargetColor;
				}
			}

			foreach (Image image in TargetLockHighlight.GetComponentsInChildren<Image>())
			{
				image.color = TargetColor;
				image.pixelsPerUnitMultiplier = 1f;
			}
		}
	}

	private void HideTargetingInterface()
	{
		_group.alpha = 0f;
		_group.interactable = false;
		_group.blocksRaycasts = false;
		TargetLockHighlight.gameObject.SetActive(false);
	}
}
}