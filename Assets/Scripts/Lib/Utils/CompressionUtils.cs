using System.IO;
using System.IO.Compression;
using System.Text;

namespace Syy1125.OberthEffect.Lib.Utils
{
public static class CompressionUtils
{
	public static byte[] Compress(string str)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(str);

		using var input = new MemoryStream(bytes);
		using var output = new MemoryStream();
		using (var gzip = new GZipStream(output, CompressionMode.Compress))
		{
			input.CopyTo(gzip);
		}

		return output.ToArray();
	}

	public static string Decompress(byte[] data)
	{
		using var input = new MemoryStream(data);
		using var output = new MemoryStream();
		using (var gzip = new GZipStream(input, CompressionMode.Decompress))
		{
			gzip.CopyTo(output);
		}

		return Encoding.UTF8.GetString(output.ToArray());
	}
}
}