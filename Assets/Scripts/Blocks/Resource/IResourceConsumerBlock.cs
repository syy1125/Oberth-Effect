using System.Collections.Generic;
using Syy1125.OberthEffect.Common;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Resource
{
public interface IResourceConsumerBlockRegistry : IBlockRegistry<IResourceConsumerBlock>, IEventSystemHandler
{}

public interface IResourceConsumerBlock
{
	IDictionary<VehicleResource, float> GetResourceConsumptionRateRequest();
	void SatisfyResourceRequestAtLevel(float level);
}
}