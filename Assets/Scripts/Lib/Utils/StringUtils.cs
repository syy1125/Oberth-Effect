using System.Linq;

namespace Syy1125.OberthEffect.Lib.Utils
{
public static class StringUtils
{
	// Takes a normal phrase and capitalize the first letter of every word.
	public static string ToTitleCase(string words)
	{
		return string.Join(
			" ",
			words.Split(' ').Select(token => token.Substring(0, 1).ToUpper() + token.Substring(1))
		);
	}
}
}