using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Resource
{
public interface IResourceGeneratorRegistry : IBlockRegistry<IResourceGenerator>
{}

public interface IResourceGenerator
{
	/// <remark>
	/// The return value on this should NOT be time-scaled.
	/// </remark>
	public IReadOnlyDictionary<string, float> GetGenerationRate();

	public IReadOnlyDictionary<string, float> GetMaxGenerationRate();
}
}