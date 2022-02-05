using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.Validation.Attributes;

namespace Syy1125.OberthEffect.Spec.Block
{
public class SoundReferenceSpec
{
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	[ValidateSoundId]
	public string SoundId;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	[ValidateRangeFloat(0f, 1f)]
	public float Volume = 1f;
}
}