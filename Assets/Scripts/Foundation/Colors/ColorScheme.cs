using System;
using System.Text;
using UnityEngine;

namespace Syy1125.OberthEffect.Foundation.Colors
{
[Serializable]
public struct ColorScheme
{
	public Color PrimaryColor;
	public Color SecondaryColor;
	public Color TertiaryColor;

	public ColorScheme(Color primary, Color secondary, Color tertiary)
	{
		PrimaryColor = primary;
		SecondaryColor = secondary;
		TertiaryColor = tertiary;
	}

	public static ColorScheme DefaultColorScheme = new(
		new Color(0f, 0.5f, 1f, 1f), Color.blue, Color.yellow
	);

	public static ColorScheme FromBlueprint(VehicleBlueprint blueprint)
	{
		return blueprint.UseCustomColors
			? blueprint.ColorScheme
			: PlayerColorScheme();
	}

	public static ColorScheme PlayerColorScheme()
	{
		return new ColorScheme(
			GetPrefColor(PropertyKeys.PRIMARY_COLOR, new Color(0f, 0.5f, 1f, 1f)),
			GetPrefColor(PropertyKeys.SECONDARY_COLOR, Color.blue),
			GetPrefColor(PropertyKeys.TERTIARY_COLOR, Color.yellow)
		);
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

	public static string ToColorSet(ColorScheme colorScheme)
	{
		return new StringBuilder()
			.Append(ColorUtility.ToHtmlStringRGB(colorScheme.PrimaryColor))
			.Append(';')
			.Append(ColorUtility.ToHtmlStringRGB(colorScheme.SecondaryColor))
			.Append(';')
			.Append(ColorUtility.ToHtmlStringRGB(colorScheme.TertiaryColor))
			.ToString();
	}

	public static bool TryParseColorSet(string colorString, out ColorScheme colorScheme)
	{
		string[] colors = colorString.Split(';');

		if (colors.Length == 3
		    && ColorUtility.TryParseHtmlString("#" + colors[0], out Color primary)
		    && ColorUtility.TryParseHtmlString("#" + colors[1], out Color secondary)
		    && ColorUtility.TryParseHtmlString("#" + colors[2], out Color tertiary))
		{
			colorScheme = new ColorScheme(primary, secondary, tertiary);
			return true;
		}
		else
		{
			colorScheme = default;
			return false;
		}
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