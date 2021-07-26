using System.Collections.Generic;
using Syy1125.OberthEffect.Common;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Resource
{
public interface IResourceGeneratorBlockRegistry : IBlockRegistry<IResourceGeneratorBlock>, IEventSystemHandler
{}

public interface IResourceGeneratorBlock
{
	/// <remark>
	/// The return value on this should NOT be time-scaled.
	/// </remark>
	public IReadOnlyDictionary<string, float> GetGenerationRate();

	public IReadOnlyDictionary<string, float> GetMaxGenerationRate();
}
}