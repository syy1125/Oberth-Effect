using Syy1125.OberthEffect.Spec.Checksum;

namespace Syy1125.OberthEffect.Spec.Block
{
public struct InfoSpec
{
	[RequireChecksumLevel(ChecksumLevel.Everything)]
	public string ShortName;
	[RequireChecksumLevel(ChecksumLevel.Everything)]
	public string FullName;
	[RequireChecksumLevel(ChecksumLevel.Everything)]
	public string Description;
}
}