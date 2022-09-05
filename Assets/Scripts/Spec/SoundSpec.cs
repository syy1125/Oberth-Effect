using System.IO;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.ModLoading;
using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;
using Syy1125.OberthEffect.Spec.Validation.Attributes;

namespace Syy1125.OberthEffect.Spec
{
[CreateSchemaFile("SoundSpecSchema")]
[ContainsPath]
public struct SoundSpec : ICustomChecksum
{
	[IdField]
	public string SoundId;
	[ValidateFilePath]
	[ResolveAbsolutePath]
	public string SoundPath;

	public void GetBytes(Stream stream, ChecksumLevel level)
	{
		if (level < ChecksumLevel.Strict) return;

		ChecksumHelper.GetBytesFromString(stream, SoundId);

		Stream sound = File.OpenRead(SoundPath);
		sound.CopyTo(stream);
	}
}
}