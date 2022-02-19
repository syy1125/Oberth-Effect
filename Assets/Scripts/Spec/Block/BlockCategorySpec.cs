using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.ModLoading;

namespace Syy1125.OberthEffect.Spec.Block
{
public class BlockCategorySpec
{
	[RequireChecksumLevel(ChecksumLevel.Everything)]
	[IdField]
	public string BlockCategoryId;
	public bool Enabled;
	[RequireChecksumLevel(ChecksumLevel.Everything)]
	public int Order;
	[RequireChecksumLevel(ChecksumLevel.Everything)]
	public string DisplayName;
	[RequireChecksumLevel(ChecksumLevel.Everything)]
	public string IconTextureId;
}
}