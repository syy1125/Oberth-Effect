using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Spec.Checksum;

namespace Syy1125.OberthEffect.Spec.Block.Weapon
{
public abstract class AbstractWeaponSpec
{
	// Choose one. Only one will be installed anyway.
	public ProjectileWeaponEffectSpec ProjectileWeaponEffect;
	public BurstBeamWeaponEffectSpec BurstBeamWeaponEffect;
	public ContinuousBeamWeaponEffectSpec ContinuousBeamWeaponEffect;
	public MissileLauncherEffectSpec MissileLauncherEffect;

	[RequireChecksumLevel(ChecksumLevel.Everything)]
	public WeaponBindingGroup DefaultBinding;
}
}