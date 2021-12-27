using Syy1125.OberthEffect.Spec.Checksum;

namespace Syy1125.OberthEffect.Spec.Block
{
public struct BlockCategorySpec
{
	[RequireChecksumLevel(ChecksumLevel.Everything)]
	public string BlockCategoryId;
	[RequireChecksumLevel(ChecksumLevel.Everything)]
	public int Order;
	[RequireChecksumLevel(ChecksumLevel.Everything)]
	public string DisplayName;
	[RequireChecksumLevel(ChecksumLevel.Everything)]
	public string IconTextureId;
}
}