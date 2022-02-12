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
	public Text TargetText;
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
			string targetName = _target.GetComponentInParent<ITargetNameProvider>()?.GetName() ?? string.Empty;
			float distance = Vector2.Distance(
				WeaponControl.GetComponent<Rigidbody2D>().worldCenterOfMass,
				_target.GetComponent<IGuidedWeaponTarget>().GetEffectivePosition()
			);

			if (WeaponControl.TargetLock)
			{
				TargetText.text = $"Target locked: {targetName}\nDistance: {PhysicsUnitUtils.FormatDistance(distance)}";
			}
			else
			{
				TargetText.text = $"Target: {targetName}\nDistance: {PhysicsUnitUtils.FormatDistance(distance)}";
			}
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
			Frame.color = TargetLockColor;
			TargetText.fontStyle = FontStyle.Bold;
			TargetText.color = TargetLockColor;

			foreach (Image image in TargetLockHighlight.GetComponentsInChildren<Image>())
			{
				image.color = TargetLockColor;
				image.pixelsPerUnitMultiplier = 0.5f;
			}
		}
		else
		{
			Frame.color = TargetColor;
			TargetText.fontStyle = FontStyle.Normal;
			TargetText.color = TargetColor;

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