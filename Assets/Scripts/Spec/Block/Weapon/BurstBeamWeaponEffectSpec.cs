using Syy1125.OberthEffect.Spec.Block.Propulsion;

namespace Syy1125.OberthEffect.Spec.Block.Weapon
{
// For burst beam weapons, Damage is interpreted as the damage done in one burst.
public class BurstBeamWeaponEffectSpec : AbstractWeaponEffectSpec
{
	public float ReloadTime;
	
	/// <summary>
	/// If true, uses precise duration behaviour. <code>BeamDurationTicks</code> is used and <code>BeamDurationSeconds</code> is ignored.
	/// If false, uses time-based duration behaviour. <code>BeamDurationSeconds</code> is used and <code>BeamDurationTicks</code> is ignored.
	/// </summary>
	public bool PreciseDuration;
	
	public int BeamDurationTicks;
	public float BeamDurationSeconds;

	public float BeamWidth;
	public string BeamColor;
	public ParticleSystemSpec[] HitParticles;
}
}