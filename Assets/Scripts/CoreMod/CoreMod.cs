using Syy1125.OberthEffect.Blocks.Propulsion;
using Syy1125.OberthEffect.CoreMod.Propulsion;
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
	}
}
}