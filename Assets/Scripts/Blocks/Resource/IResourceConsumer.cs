﻿using System.Collections.Generic;

namespace Syy1125.OberthEffect.Blocks.Resource
{
public interface IResourceConsumerRegistry : IBlockRegistry<IResourceConsumer>
{}

public interface IResourceConsumer
{
	IReadOnlyDictionary<string, float> GetMaxResourceUseRate();
	IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest();
	void SatisfyResourceRequestAtLevel(float level);
}
}