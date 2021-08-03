using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Simulation.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
public class BlockHealthBar : MonoBehaviour
{
	public BlockHealth Target;
	public Image HealthBar;
	public Gradient ColorGradient;

	private BlockCore _blockCore;
	private VehicleCore _vehicleCore;

	private void Start()
	{
		_blockCore = Target.GetComponent<BlockCore>();
		_vehicleCore = Target.GetComponentInParent<VehicleCore>();
	}

	private void LateUpdate()
	{
		float healthFraction = Target.HealthFraction;
		HealthBar.fillAmount = healthFraction;
		HealthBar.color = ColorGradient.Evaluate(healthFraction);

		Vector3 targetPosition = _vehicleCore.transform.TransformPoint(_blockCore.CenterOfMassPosition);
		targetPosition.z = transform.position.z;
		transform.position = targetPosition;
	}
}
}