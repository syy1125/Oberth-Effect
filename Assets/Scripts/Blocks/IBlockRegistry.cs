namespace Syy1125.OberthEffect.Blocks
{
public interface IBlockRegistry<in T>
{
	void RegisterBlock(T block);
	void UnregisterBlock(T block);
}
}