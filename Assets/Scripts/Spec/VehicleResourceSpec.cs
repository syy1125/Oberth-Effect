using Syy1125.OberthEffect.Spec.Checksum;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec
{
public struct VehicleResourceSpec
{
	public string ResourceId;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public string DisplayName;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public string ShortName;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public string DisplayColor;

	public string WrapColorTag(string content) => $"<color=\"{DisplayColor}\">{content}</color>";

	public Color GetDisplayColor() =>
		ColorUtility.TryParseHtmlString(DisplayColor, out Color color) ? color : Color.white;
}
}