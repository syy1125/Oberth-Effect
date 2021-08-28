using System.Collections.Generic;
using Syy1125.OberthEffect.Spec.Unity;

namespace Syy1125.OberthEffect.Spec.Block.Resource
{
public struct FusionGeneratorSpec
{
	public Dictionary<string, float> GenerationRate;
	public RendererSpec[] ActiveRenderers;
}
}