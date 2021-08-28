using System.Collections.Generic;

namespace Syy1125.OberthEffect.Spec.Block.Resource
{
public class ResourceSpec
{
	public Dictionary<string, float> StorageCapacity;
	public Dictionary<string, float> FreeGenerator;
	public FusionGeneratorSpec FusionGenerator;
}
}