using Photon.Pun;
using UnityEngine;

namespace Syy1125.OberthEffect.Foundation.Colors
{
public class ColorContext : MonoBehaviour, IPunInstantiateMagicCallback
{
	public delegate void ColorChangeEvent(Color color);

	private ColorScheme _colorScheme = ColorScheme.DefaultColorScheme;
	public ColorScheme ColorScheme => _colorScheme;

	public event ColorChangeEvent OnPrimaryColorChanged;
	public event ColorChangeEvent OnSecondaryColorChanged;
	public event ColorChangeEvent OnTertiaryColorChanged;

	public void SetPrimaryColor(Color color)
	{
		_colorScheme.PrimaryColor = color;
		OnPrimaryColorChanged?.Invoke(color);
	}

	public void SetSecondaryColor(Color color)
	{
		_colorScheme.SecondaryColor = color;
		OnSecondaryColorChanged?.Invoke(color);
	}

	public void SetTertiaryColor(Color color)
	{
		_colorScheme.TertiaryColor = color;
		OnTertiaryColorChanged?.Invoke(color);
	}

	public void SetColorScheme(ColorScheme colorScheme)
	{
		_colorScheme = colorScheme;
		OnPrimaryColorChanged?.Invoke(colorScheme.PrimaryColor);
		OnSecondaryColorChanged?.Invoke(colorScheme.SecondaryColor);
		OnTertiaryColorChanged?.Invoke(colorScheme.TertiaryColor);
	}

	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		object[] instantiationData = info.photonView.InstantiationData;
		var colorScheme = JsonUtility.FromJson<ColorScheme>((string) instantiationData[1]);
		SetColorScheme(colorScheme);
	}
}
}