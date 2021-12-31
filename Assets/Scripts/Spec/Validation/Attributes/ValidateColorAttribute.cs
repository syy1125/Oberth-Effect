using System.Linq;

namespace Syy1125.OberthEffect.Spec.Validation.Attributes
{
public class ValidateColorAttribute : AbstractValidationAttribute
{
	private readonly bool _allowColorScheme;

	public ValidateColorAttribute(bool allowColorScheme)
	{
		_allowColorScheme = allowColorScheme;
	}

	public override void Validate(object value)
	{
		if (value == null) return;
		
		if (value is string stringValue)
		{
			switch (stringValue.ToLower())
			{
				case "primary":
				case "secondary":
				case "tertiary":
					if (!_allowColorScheme)
					{
						throw new ValidationError("should not reference color scheme");
					}

					break;

				// Valid html colors: https://docs.unity3d.com/ScriptReference/ColorUtility.TryParseHtmlString.html
				// Unity's ColorUtility.TryParseHtmlString is not thread safe
				case "red":
				case "cyan":
				case "blue":
				case "darkblue":
				case "lightblue":
				case "purple":
				case "yellow":
				case "lime":
				case "fuchsia":
				case "white":
				case "silver":
				case "grey":
				case "black":
				case "orange":
				case "brown":
				case "maroon":
				case "green":
				case "olive":
				case "navy":
				case "teal":
				case "aqua":
				case "magenta":
					break;

				default:
					if (stringValue.StartsWith("#"))
					{
						if (
							stringValue.Length == 4
							|| stringValue.Length == 7
							|| stringValue.Length == 5
							|| stringValue.Length == 9
						)
						{
							if (stringValue.Substring(1).ToCharArray().All(IsValidHexChar))
							{
								break;
							}
						}
					}

					throw new ValidationError("should be a color string");
			}
		}
		else
		{
			throw new ValidationError("should be a color string");
		}
	}

	private static bool IsValidHexChar(char c)
	{
		switch (char.ToLower(c))
		{
			case '0':
			case '1':
			case '2':
			case '3':
			case '4':
			case '5':
			case '6':
			case '7':
			case '8':
			case '9':
			case 'a':
			case 'b':
			case 'c':
			case 'd':
			case 'e':
			case 'f':
				return true;
			default:
				return false;
		}
	}
}
}