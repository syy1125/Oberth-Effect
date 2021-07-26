namespace Syy1125.OberthEffect.Spec
{
public struct VehicleResourceSpec
{
	public string ResourceId;
	public string DisplayName;
	public string ShortName;
	public string DisplayColor;

	public string WrapColorTag(string content) => $"<color=\"{DisplayColor}\">{content}</color>";
}
}