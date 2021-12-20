using System;
using UnityEngine;

namespace Syy1125.OberthEffect.Common.Colors
{
[Serializable]
public struct ColorScheme
{
	public Color PrimaryColor;
	public Color SecondaryColor;
	public Color TertiaryColor;

	public static ColorScheme DefaultColorScheme = new ColorScheme
	{
		PrimaryColor = new Color(0f, 0.5f, 1f, 1f),
		SecondaryColor = Color.blue,
		TertiaryColor = Color.yellow,
	};

	public static ColorScheme FromBlueprint(VehicleBlueprint blueprint)
	{
		return blueprint.UseCustomColors
			? blueprint.ColorScheme
			: PlayerColorScheme();
	}

	public static ColorScheme PlayerColorScheme()
	{
		return new ColorScheme
		{
			PrimaryColor = GetPrefColor(PropertyKeys.PRIMARY_COLOR, Color.cyan),
			SecondaryColor = GetPrefColor(PropertyKeys.SECONDARY_COLOR, Color.blue),
			TertiaryColor = GetPrefColor(PropertyKeys.TERTIARY_COLOR, Color.yellow)
		};
	}

	public static Color GetPrefColor(string prefKey, Color defaultColor)
	{
		string prefColorString = PlayerPrefs.GetString(prefKey);
		Color prefColor = string.IsNullOrWhiteSpace(prefColorString)
			? defaultColor
			: JsonUtility.FromJson<Color>(prefColorString);
		prefColor.a = 1f;
		return prefColor;
	}

	public bool ResolveColor(string colorString, out Color color)
	{
		switch (colorString.ToLower())
		{
			case "primary":
				color = PrimaryColor;
				return true;
			case "secondary":
				color = SecondaryColor;
				return true;
			case "tertiary":
				color = TertiaryColor;
				return true;
			default:
				return ColorUtility.TryParseHtmlString(colorString, out color);
		}
	}
}
}