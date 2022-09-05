using Syy1125.OberthEffect.Blocks;

namespace Syy1125.OberthEffect.Spec.Block
{
public interface IBlockComponent<in TSpec>
{
	/// <summary>
	/// Configure the component using the supplied spec object. This happens immediately after instantiation, between Enable and Start.
	/// </summary>
	void LoadSpec(TSpec spec, in BlockContext context);
}
}