using System.IO;

namespace Syy1125.OberthEffect.Spec.Checksum
{
public interface ICustomChecksum
{
	void GetBytes(Stream stream, ChecksumLevel level);
}
}