using System.Collections.Generic;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Unity;

namespace Syy1125.OberthEffect.Spec.Block.Propulsion
{
public class LinearEngineSpec
{
	public float MaxForce;
	public Dictionary<string, float> MaxResourceUse;
	public float MaxThrottleRate;
	public ControlConditionSpec ActivationCondition;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public ParticleSystemSpec[] Particles;
}
}