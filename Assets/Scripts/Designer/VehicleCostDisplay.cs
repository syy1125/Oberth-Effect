using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Designer
{
public class VehicleCostDisplay : MonoBehaviour
{
	public VehicleBuilder Builder;
	public Text CostDisplay;

	private void Update()
	{
		CostDisplay.text = $"Cost {Builder.VehicleCost}";
	}
}
}