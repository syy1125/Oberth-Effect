using Syy1125.OberthEffect.Blocks.Propulsion;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.CombatSystem;
using Syy1125.OberthEffect.CoreMod.Propulsion;
using Syy1125.OberthEffect.CoreMod.Resource;
using Syy1125.OberthEffect.CoreMod.Weapons;
using Syy1125.OberthEffect.CoreMod.Weapons.GuidanceSystem;
using Syy1125.OberthEffect.Spec.Block;

namespace Syy1125.OberthEffect.CoreMod
{
public static class CoreMod
{
	public static void Init()
	{
		BlockSpec.Register<VolatileSpec, VolatileBlock>("Volatile");
		BlockSpec.Register<OmniThrusterSpec, OmniThruster>("OmniThruster");
		BlockSpec.Register<DirectionalThrusterSpec, DirectionalThruster>("DirectionalThruster");
		BlockSpec.Register<LinearEngineSpec, LinearEngine>("LinearEngine");
		BlockSpec.Register<ReactionWheelSpec, ReactionWheel>("ReactionWheel");
		BlockSpec.Register<ResourceStorageSpec, ResourceStorage>("ResourceStorage");
		BlockSpec.Register<ResourceGeneratorSpec, ResourceGenerator>("ResourceGenerator");
		BlockSpec.Register<TurretedWeaponSpec, TurretedWeapon>("TurretedWeapon");
		BlockSpec.Register<FixedWeaponSpec, FixedWeapon>("FixedWeapon");

		NetworkedProjectileConfig
			.Register<PredictiveGuidanceSystemSpec, PredictiveGuidanceSystem>("PredictiveGuidance");
	}
}
}