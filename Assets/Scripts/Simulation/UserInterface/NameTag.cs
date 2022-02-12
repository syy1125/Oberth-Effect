using System;
using Photon.Pun;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Simulation.Construct;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
public class NameTag : MonoBehaviour
{
	public Text NameTagText;
	[NonSerialized]
	public VehicleCore Target;

	private Camera _mainCamera;
	private RectTransform _parentTransform;

	private void Awake()
	{
		_mainCamera = Camera.main;
		_parentTransform = transform.parent.GetComponent<RectTransform>();
	}

	private void Start()
	{
		PhotonView photonView = Target.GetComponent<PhotonView>();
		NameTagText.text = photonView.Owner.NickName;

		Color color = PhotonTeamHelper.GetPlayerTeamColors(photonView.Owner).PrimaryColor;
		NameTagText.color = color;
		Color.RGBToHSV(color, out float h, out float s, out float v);
		h = (h + 0.5f) % 1;
		NameTagText.GetComponent<Outline>().effectColor = Color.HSVToRGB(h, s, v);
	}

	private void Update()
	{
		if (Target == null || Target.IsDead)
		{
			Destroy(gameObject);
		}
	}

	private void LateUpdate()
	{
		if (Target == null) return;

		Vector3 targetPosition = Target.GetComponent<Rigidbody2D>().worldCenterOfMass;
		Vector2 screenPosition = _mainCamera.WorldToScreenPoint(targetPosition);

		if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
			    _parentTransform, screenPosition, null, out Vector2 localPoint
		    ))
		{
			transform.localPosition = localPoint;
		}
		else
		{
			Debug.LogError("Failed to calculate local position for name tag!");
		}
	}
}
}