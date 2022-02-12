using System.Linq;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Spec.Database;

namespace Syy1125.OberthEffect.Common
{
public static class VehicleHelper
{
	public static int GetCost(VehicleBlueprint vehicle)
	{
		return vehicle.Blocks.Sum(instance => BlockDatabase.Instance.GetBlockSpec(instance.BlockId).Cost);
	}
}
}