using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Spec.Checksum;

namespace Syy1125.OberthEffect.Spec.Block.Weapon
{
public class FixedWeaponSpec
{
	public ProjectileWeaponEffectSpec ProjectileWeaponEffect;
	public BurstBeamWeaponEffectSpec BurstBeamWeaponEffect;
	public ContinuousBeamWeaponEffectSpec ContinuousBeamWeaponEffect;

	[RequireChecksumLevel(ChecksumLevel.Everything)]
	public WeaponBindingGroup DefaultBinding;
}
}