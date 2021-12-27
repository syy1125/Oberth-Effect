using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.Unity;

namespace Syy1125.OberthEffect.Spec.Block.Weapon
{
// For continuous beam weapons, damage is interpreted as damage per second.
public class ContinuousBeamWeaponEffectSpec : AbstractWeaponEffectSpec
{
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public float BeamWidth;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public string BeamColor;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public ParticleSystemSpec[] HitParticles;
}
}