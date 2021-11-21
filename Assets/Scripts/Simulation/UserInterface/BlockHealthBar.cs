using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Simulation.Construct;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
public class BlockHealthBar : MonoBehaviour
{
	public BlockHealth Target;
	public Image HealthBar;
	public Gradient ColorGradient;

	private Camera _mainCamera;
	private RectTransform _parentTransform;
	private BlockCore _blockCore;
	private VehicleCore _vehicleCore;

	private void Start()
	{
		_mainCamera = Camera.main;
		_parentTransform = transform.parent.GetComponent<RectTransform>();
		_blockCore = Target.GetComponent<BlockCore>();
		_vehicleCore = Target.GetComponentInParent<VehicleCore>();
	}

	private void LateUpdate()
	{
		float healthFraction = Target.HealthFraction;
		HealthBar.fillAmount = healthFraction;
		HealthBar.color = ColorGradient.Evaluate(healthFraction);

		Vector3 targetPosition = _vehicleCore.transform.TransformPoint(_blockCore.CenterOfMassPosition);
		Vector2 screenPosition = _mainCamera.WorldToScreenPoint(targetPosition);

		if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
			_parentTransform, screenPosition, null, out Vector2 localPoint
		))
		{
			transform.localPosition = localPoint;
		}
		else
		{
			Debug.LogError("Failed to calculate local position for health bar!");
		}
	}
}
}