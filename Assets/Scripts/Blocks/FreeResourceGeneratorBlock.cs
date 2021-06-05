using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Common;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
public class FreeResourceGeneratorBlock : ResourceGeneratorBlock
{
	public ResourceEntry[] GenerationRate;

	public override Dictionary<VehicleResource, float> GenerateResources()
	{
		return GenerationRate.ToDictionary(
			entry => entry.Resource,
			entry => entry.Amount * Time.fixedDeltaTime
		);
	}
}
}