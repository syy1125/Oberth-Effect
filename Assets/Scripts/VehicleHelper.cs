using System.Linq;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Spec.Database;

namespace Syy1125.OberthEffect
{
public static class VehicleHelper
{
	public static int GetCost(VehicleBlueprint vehicle)
	{
		return vehicle.Blocks.Sum(instance => BlockDatabase.Instance.GetSpecInstance(instance.BlockId).Spec.Cost);
	}
}
}