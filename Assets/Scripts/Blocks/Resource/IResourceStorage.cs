using System.Collections.Generic;

namespace Syy1125.OberthEffect.Blocks.Resource
{
public interface IResourceStorageRegistry : IBlockRegistry<IResourceStorage>
{}

public interface IResourceStorage
{
	IReadOnlyDictionary<string, float> GetCapacity();
}
}