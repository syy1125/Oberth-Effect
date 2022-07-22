using Syy1125.OberthEffect.CoreMod.Weapons.Launcher;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.Unity;
using Syy1125.OberthEffect.Spec.Validation.Attributes;

namespace Syy1125.OberthEffect.Spec.Block.Weapon
{
// For burst beam weapons, Damage is interpreted as the damage done in one burst.
public class BurstBeamLauncherSpec : AbstractWeaponLauncherSpec
{
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float ReloadTime;

	/// <summary>
	/// If true, uses precise duration behaviour. <code>BeamDurationTicks</code> is used and <code>BeamDurationSeconds</code> is ignored.
	/// If false, uses time-based duration behaviour. <code>BeamDurationSeconds</code> is used and <code>BeamDurationTicks</code> is ignored.
	/// </summary>
	public bool PreciseDuration;
	[ValidateRangeInt(0, int.MaxValue)]
	public int DurationTicks;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float DurationSeconds;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxRange;

	[RequireChecksumLevel(ChecksumLevel.Strict)]
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float BeamWidth;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	[ValidateColor(true)]
	public string BeamColor;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public ParticleSystemSpec[] HitParticles;
}
}