using Syy1125.OberthEffect.Blocks;

namespace Syy1125.OberthEffect.Spec.Block
{
public interface IBlockComponent<in TSpec>
{
	void LoadSpec(TSpec spec, in BlockContext context);
}
}