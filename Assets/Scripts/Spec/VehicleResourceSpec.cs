using UnityEngine;

namespace Syy1125.OberthEffect.Spec
{
public struct VehicleResourceSpec
{
	public string ResourceId;
	public string DisplayName;
	public string ShortName;
	public string DisplayColor;

	public string WrapColorTag(string content) => $"<color=\"{DisplayColor}\">{content}</color>";

	public Color GetDisplayColor() =>
		ColorUtility.TryParseHtmlString(DisplayColor, out Color color) ? color : Color.white;
}
}