using Photon.Pun;
using Syy1125.OberthEffect.Foundation.Utils;
using Syy1125.OberthEffect.Simulation.Construct;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
[RequireComponent(typeof(CanvasGroup))]
public class TargetInterface : MonoBehaviour
{
	[Header("References")]
	public CameraFollow CameraFollow;
	public Image Frame;
	public Text TargetNameText;
	public Text TargetInfoText;
	public Transform RelativeVelocityDirection;
	public Text BrakingBurnText;
	public Text ClosestApproachText;
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
		GameObject vehicle = PlayerVehicleSpawner.Instance.Vehicle;

		if (vehicle == null)
		{
			UpdateTarget(null, false);
			return;
		}

		VehicleWeaponControl weaponControl = PlayerVehicleSpawner.Instance.Vehicle.GetComponent<VehicleWeaponControl>();

		if (weaponControl.TargetPhotonViewId != _targetId || weaponControl.TargetLock != _targetLock)
		{
			UpdateTarget(weaponControl.TargetPhotonViewId, weaponControl.TargetLock);
		}

		if (_target != null)
		{
			Rigidbody2D vehicleBody = weaponControl.GetComponent<Rigidbody2D>();
			ITargetLockInfoProvider infoProvider = _target.GetComponent<ITargetLockInfoProvider>();

			string targetName = infoProvider.GetName();
			Vector2 relativePosition = infoProvider.GetPosition() - vehicleBody.worldCenterOfMass;
			Vector2 relativeVelocity = infoProvider.GetVelocity() - vehicleBody.velocity;
			TargetNameText.text = weaponControl.TargetLock ? $"Target locked: {targetName}" : $"Target: {targetName}";
			TargetInfoText.text =
				$"Distance: {PhysicsUnitUtils.FormatDistance(relativePosition.magnitude)}, RVel: {PhysicsUnitUtils.FormatSpeed(relativeVelocity.magnitude)}";
			RelativeVelocityDirection.rotation = Quaternion.LookRotation(Vector3.forward, relativeVelocity);
			RelativeVelocityDirection.gameObject.SetActive(relativeVelocity.sqrMagnitude > 0.01f);

			UpdateBrakingBurnDisplay(vehicle, relativePosition, relativeVelocity);
		}
	}

	private void UpdateBrakingBurnDisplay(GameObject vehicle, Vector2 relativePosition, Vector2 relativeVelocity)
	{
		if (relativeVelocity.sqrMagnitude < 0.01f)
		{
			BrakingBurnText.text = "";
			ClosestApproachText.text = "";
			return;
		}

		float maxAcceleration = vehicle.GetComponent<VehicleThrusterControl>().GetMaxForwardThrust()
		                        / vehicle.GetComponent<Rigidbody2D>().mass;

		if (Mathf.Approximately(maxAcceleration, 0f))
		{
			BrakingBurnText.text = "";
			ClosestApproachText.text = "";
			return;
		}

		float brakingBurnTime = relativeVelocity.magnitude / maxAcceleration;
		Vector2 closestApproach = Vector3.ProjectOnPlane(relativePosition, relativeVelocity);
		float brakingBurnDistance = 0.5f * relativeVelocity.sqrMagnitude / maxAcceleration;
		float brakingBurnCountdown = (Vector2.Distance(relativePosition, closestApproach) - brakingBurnDistance)
		                             / relativeVelocity.magnitude;

		BrakingBurnText.text = $"Est. braking burn of {brakingBurnTime:F1}s starting in {brakingBurnCountdown:F1}s";
		ClosestApproachText.text = $"Closest approach {PhysicsUnitUtils.FormatDistance(closestApproach.magnitude)}";
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

		if (targetLock)
		{
			foreach (Text text in GetComponentsInChildren<Text>())
			{
				text.fontStyle = FontStyle.Bold;
			}

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
			foreach (Text text in GetComponentsInChildren<Text>())
			{
				text.fontStyle = FontStyle.Normal;
			}

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