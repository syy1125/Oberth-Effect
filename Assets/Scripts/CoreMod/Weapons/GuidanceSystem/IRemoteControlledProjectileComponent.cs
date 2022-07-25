using Syy1125.OberthEffect.CoreMod.Weapons.Launcher;

namespace Syy1125.OberthEffect.CoreMod.Weapons.GuidanceSystem
{
public interface IRemoteControlledProjectileComponent
{
	/// <summary>
	/// The launcher that "owns" this projectile. To reduce code complexity, this is only set on the client that created this projectile.
	/// </summary>
	AbstractWeaponLauncher Launcher { set; }
}
}