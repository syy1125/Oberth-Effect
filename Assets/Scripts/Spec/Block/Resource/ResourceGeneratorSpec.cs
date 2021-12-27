using System.Collections.Generic;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Unity;

namespace Syy1125.OberthEffect.Spec.Block.Resource
{
public class ResourceGeneratorSpec
{
	public Dictionary<string, float> ConsumptionRate;
	public Dictionary<string, float> GenerationRate;
	public ControlConditionSpec ActivationCondition;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public RendererSpec[] ActivationRenderers;
}
}