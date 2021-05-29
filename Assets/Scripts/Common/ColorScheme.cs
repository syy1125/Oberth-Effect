﻿using System;
using UnityEngine;

namespace Syy1125.OberthEffect.Common
{
[Serializable]
public struct ColorScheme
{
	public Color PrimaryColor;
	public Color SecondaryColor;
	public Color TertiaryColor;

	public static ColorScheme DefaultColorScheme = new ColorScheme
	{
		PrimaryColor = Color.cyan,
		SecondaryColor = Color.blue,
		TertiaryColor = Color.yellow,
	};

	public static ColorScheme FromBlueprint(VehicleBlueprint blueprint)
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
}
}