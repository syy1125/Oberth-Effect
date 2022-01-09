using System;
using Photon.Pun;
using Syy1125.OberthEffect.Simulation.Construct;
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

	[Header("Config")]
	public Color TargetColor = new Color(0.75f, 0f, 0f);
	public Color TargetLockColor = Color.red;

	private CanvasGroup _group;
	private int? _targetId;
	private bool _targetLock;

	private void Awake()
	{
		_group = GetComponent<CanvasGroup>();
	}

	private void Start()
	{
		SetTargetId(null, false);
	}

	private void Update()
	{
		if (WeaponControl == null || !WeaponControl.enabled)
		{
			SetTargetId(null, false);
			return;
		}

		if (WeaponControl.TargetPhotonId != _targetId || WeaponControl.TargetLock != _targetLock)
		{
			SetTargetId(WeaponControl.TargetPhotonId, WeaponControl.TargetLock);
		}
	}

	private void SetTargetId(int? targetId, bool targetLock)
	{
		_targetId = targetId;
		_targetLock = targetLock;

		if (_targetId == null)
		{
			_group.alpha = 0f;
			_group.interactable = false;
			_group.blocksRaycasts = false;
		}
		else
		{
			GameObject target = PhotonView.Find(_targetId.Value).gameObject;

			CameraFollow.Target = target.transform;
			_group.alpha = 1f;
			_group.interactable = true;
			_group.blocksRaycasts = true;

			string targetName =
				$"{target.GetComponent<PhotonView>().Owner.NickName} ({target.GetComponent<VehicleCore>().VehicleName})";

			if (WeaponControl.TargetLock)
			{
				Frame.color = TargetLockColor;
				TargetText.text =
					$"<b><color=\"#{ColorUtility.ToHtmlStringRGB(TargetLockColor)}\">Target locked: {targetName}</color></b>";
			}
			else
			{
				Frame.color = TargetColor;
				TargetText.text =
					$"<color=\"#{ColorUtility.ToHtmlStringRGB(TargetColor)}\">Target: {targetName}</color>";
			}
		}
	}
}
}