using UnityEngine;

namespace Syy1125.OberthEffect.Common
{
public class ColorContext : MonoBehaviour
{
	public delegate void ColorChangeEvent(Color color);

	public Color PrimaryColor { get; private set; }
	public Color SecondaryColor { get; private set; }
	public Color TertiaryColor { get; private set; }

	public ColorChangeEvent OnPrimaryColorChanged;
	public ColorChangeEvent OnSecondaryColorChanged;
	public ColorChangeEvent OnTertiaryColorChanged;

	public void SetPrimaryColor(Color color)
	{
		PrimaryColor = color;
		OnPrimaryColorChanged?.Invoke(color);
	}

	public void SetSecondaryColor(Color color)
	{
		SecondaryColor = color;
		OnSecondaryColorChanged?.Invoke(color);
	}

	public void SetTertiaryColor(Color color)
	{
		TertiaryColor = color;
		OnTertiaryColorChanged?.Invoke(color);
	}
}
}