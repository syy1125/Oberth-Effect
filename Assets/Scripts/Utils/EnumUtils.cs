using System;
using System.Text;
using UnityEngine;

namespace Syy1125.OberthEffect.Utils
{
public static class EnumUtils
{
	public static string[] FormatNames(Type enumType)
	{
		string[] names = Enum.GetNames(enumType);
		string[] formattedNames = new string[names.Length];

		for (int i = 0; i < names.Length; i++)
		{
			formattedNames[i] = FormatPascalCase(names[i]);
		}

		return formattedNames;
	}

	private static string FormatPascalCase(string pascalCase)
	{
		if (pascalCase.Length <= 1) return pascalCase;

		StringBuilder builder = new StringBuilder();
		builder.Append(pascalCase[0]);
		for (int i = 1; i < pascalCase.Length; i++)
		{
			if (char.IsUpper(pascalCase[i]))
			{
				builder.Append(' ');
			}

			builder.Append(pascalCase[i]);
		}

		return builder.ToString();
	}
}
}