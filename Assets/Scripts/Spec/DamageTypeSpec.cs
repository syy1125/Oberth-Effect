using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.ModLoading;
using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec
{
[CreateSchemaFile("DamageTypeSpecSchema")]
public struct DamageTypeSpec
{
	[IdField]
	public string DamageTypeId;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public string DisplayName;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	[ValidateColor(false)]
	public string DisplayColor;

	public string WrapColorTag(string content) => $"<color=\"{DisplayColor}\">{content}</color>";

	public Color GetDisplayColor() =>
		ColorUtility.TryParseHtmlString(DisplayColor, out Color color) ? color : Color.white;
}
}