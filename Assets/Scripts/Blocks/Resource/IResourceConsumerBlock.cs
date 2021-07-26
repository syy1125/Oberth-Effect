using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Resource
{
public interface IResourceConsumerBlockRegistry : IBlockRegistry<IResourceConsumerBlock>, IEventSystemHandler
{}

public interface IResourceConsumerBlock
{
	IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest();
	void SatisfyResourceRequestAtLevel(float level);
}
}