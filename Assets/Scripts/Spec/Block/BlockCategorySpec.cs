using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.ModLoading;
using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;
using Syy1125.OberthEffect.Spec.Validation.Attributes;

namespace Syy1125.OberthEffect.Spec.Block
{
[CreateSchemaFile("BlockCategorySpecSchema")]
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