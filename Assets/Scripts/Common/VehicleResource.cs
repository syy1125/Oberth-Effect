using System;
using UnityEngine;

namespace Syy1125.OberthEffect.Common
{
[CreateAssetMenu(menuName = "Scriptable Objects/Vehicle Resource", fileName = "Resource")]
public class VehicleResource : ScriptableObject
{
	public string Id;
	public string DisplayName;
	public string ShortName;
	public Color DisplayColor;

	public string RichTextColoredName() =>
		$"<color=\"#{ColorUtility.ToHtmlStringRGB(DisplayColor)}\">{DisplayName}</color>";
}

[Serializable]
public struct ResourceEntry
{
	public VehicleResource Resource;
	public float Amount;

	public string RichTextColoredEntry() =>
		$"<color=\"#{ColorUtility.ToHtmlStringRGB(Resource.DisplayColor)}\">{Amount} {Resource.DisplayName}</color>";
}
}