using System.Collections.Generic;

namespace Syy1125.OberthEffect.Blocks.Resource
{
public interface IResourceConsumerRegistry : IBlockRegistry<IResourceConsumer>
{}

public interface IResourceConsumer
{
	IReadOnlyDictionary<string, float> GetMaxResourceUseRate();
	IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest();
	/// <returns>Actual consumption level of resources</returns>
	float SatisfyResourceRequestAtLevel(float level);
}
}