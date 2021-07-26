using System.Collections.Generic;

namespace Syy1125.OberthEffect.Spec.Block.Propulsion
{
public class LinearEngineSpec
{
	public float MaxForce;
	public Dictionary<string, float> MaxResourceUse;
	public bool IsFuelPropulsion;
	public float MaxThrottleRate;
	public ParticleSystemSpec[] Particles;
}
}