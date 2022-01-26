using System.Collections.Generic;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Unity;
using Syy1125.OberthEffect.Spec.Validation;
using Syy1125.OberthEffect.Spec.Validation.Attributes;

namespace Syy1125.OberthEffect.Spec.Block.Propulsion
{
public class DirectionalThrusterModuleSpec : ICustomValidation
{
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxForce;
	public Dictionary<string, float> MaxResourceUse;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public ParticleSystemSpec[] Particles;

	public void Validate(List<string> path, List<string> errors)
	{
		ValidationHelper.ValidateFields(path, this, errors);
		path.Add(nameof(MaxResourceUse));
		ValidationHelper.ValidateResourceDictionary(path, MaxResourceUse, errors);
		path.RemoveAt(path.Count - 1);
	}
}

public class DirectionalThrusterSpec
{
	public DirectionalThrusterModuleSpec Up;
	public DirectionalThrusterModuleSpec Down;
	public DirectionalThrusterModuleSpec Left;
	public DirectionalThrusterModuleSpec Right;

	public ControlConditionSpec ActivationCondition;
}
}