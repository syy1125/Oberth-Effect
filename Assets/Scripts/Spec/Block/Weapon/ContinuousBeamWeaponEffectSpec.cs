using Syy1125.OberthEffect.Spec.Unity;

namespace Syy1125.OberthEffect.Spec.Block.Weapon
{
// For continuous beam weapons, damage is interpreted as damage per second.
public class ContinuousBeamWeaponEffectSpec : AbstractWeaponEffectSpec
{
	public float BeamWidth;
	public string BeamColor;
	public ParticleSystemSpec[] HitParticles;
}
}