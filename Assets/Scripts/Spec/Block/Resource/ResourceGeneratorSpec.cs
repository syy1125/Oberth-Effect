using System.Collections.Generic;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Unity;
using Syy1125.OberthEffect.Spec.Validation;

namespace Syy1125.OberthEffect.Spec.Block.Resource
{
public class ResourceGeneratorSpec : ICustomValidation
{
	public Dictionary<string, float> ConsumptionRate;
	public Dictionary<string, float> GenerationRate;
	public ControlConditionSpec ActivationCondition;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public RendererSpec[] ActivationRenderers;

	public void Validate(List<string> path, List<string> errors)
	{
		ValidationHelper.ValidateFields(path, this, errors);
		path.Add(nameof(ConsumptionRate));
		ValidationHelper.ValidateResourceDictionary(path, ConsumptionRate, errors);
		path.RemoveAt(path.Count - 1);
		path.Add(nameof(GenerationRate));
		ValidationHelper.ValidateResourceDictionary(path, GenerationRate, errors);
		path.RemoveAt(path.Count - 1);
	}
}
}