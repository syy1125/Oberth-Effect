using Syy1125.OberthEffect.Common;
using UnityEngine;

namespace Syy1125.OberthEffect.Designer
{
public class CraftConfig : MonoBehaviour
{
	[Header("Config")]
	public Color DefaultPrimaryColor;
	public Color DefaultSecondaryColor;
	public Color DefaultTertiaryColor;

	[Header("References")]
	public ColorPicker PrimaryColorPicker;
	public ColorPicker SecondaryColorPicker;
	public ColorPicker TertiaryColorPicker;

	private ColorContext _context;

	private void Awake()
	{
		_context = GetComponentInParent<ColorContext>();

		Color primaryColor = InitColor(PrimaryColorPicker, PropertyKeys.PRIMARY_COLOR, DefaultPrimaryColor);
		_context.SetPrimaryColor(primaryColor);
		Color secondaryColor = InitColor(SecondaryColorPicker, PropertyKeys.SECONDARY_COLOR, DefaultSecondaryColor);
		_context.SetSecondaryColor(secondaryColor);
		Color tertiaryColor = InitColor(TertiaryColorPicker, PropertyKeys.TERTIARY_COLOR, DefaultTertiaryColor);
		_context.SetTertiaryColor(tertiaryColor);
	}

	private void OnEnable()
	{
		PrimaryColorPicker.OnChange.AddListener(SetPrimaryColor);
		SecondaryColorPicker.OnChange.AddListener(SetSecondaryColor);
		TertiaryColorPicker.OnChange.AddListener(SetTertiaryColor);
	}

	private void OnDisable()
	{
		PrimaryColorPicker.OnChange.RemoveListener(SetPrimaryColor);
		SecondaryColorPicker.OnChange.RemoveListener(SetSecondaryColor);
		TertiaryColorPicker.OnChange.RemoveListener(SetTertiaryColor);
	}

	private static Color InitColor(ColorPicker picker, string key, Color defaultColor)
	{
		string prefColorString = PlayerPrefs.GetString(key);
		Color prefColor = string.IsNullOrWhiteSpace(prefColorString)
			? defaultColor
			: JsonUtility.FromJson<Color>(prefColorString);
		prefColor.a = 1f;

		picker.InitColor(prefColor);
		return prefColor;
	}

	private void SetPrimaryColor(Color color)
	{
		_context.SetPrimaryColor(color);
		PlayerPrefs.SetString(PropertyKeys.PRIMARY_COLOR, JsonUtility.ToJson(color));
	}

	private void SetSecondaryColor(Color color)
	{
		_context.SetSecondaryColor(color);
		PlayerPrefs.SetString(PropertyKeys.SECONDARY_COLOR, JsonUtility.ToJson(color));
	}

	private void SetTertiaryColor(Color color)
	{
		_context.SetTertiaryColor(color);
		PlayerPrefs.SetString(PropertyKeys.TERTIARY_COLOR, JsonUtility.ToJson(color));
	}
}
}