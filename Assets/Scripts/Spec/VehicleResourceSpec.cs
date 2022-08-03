using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.ModLoading;
using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec
{
[CreateSchemaFile("VehicleResourceSpecSchema")]
public struct VehicleResourceSpec
{
	[IdField]
	public string ResourceId;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	[SchemaDescription("Full name of the resource.")]
	public string DisplayName;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	[SchemaDescription("A single letter representation of the resource for use in UI.")]
	public string ShortName;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	[ValidateColor(false)]
	public string DisplayColor;

	public string WrapColorTag(string content) => $"<color=\"{DisplayColor}\">{content}</color>";

	public Color GetDisplayColor() =>
		ColorUtility.TryParseHtmlString(DisplayColor, out Color color) ? color : Color.white;
}
}