using System;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Guide;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Syy1125.OberthEffect.MainMenu
{
public class MainMenu : MonoBehaviour
{
	public SceneReference Designer;
	public SceneReference GameplayGuide;
	public SceneReference MultiplayerLobby;

	public TextAsset GuideVehicle;

	public void ToDesigner()
	{
		SceneManager.LoadScene(Designer);
	}

	public void ToGuide(int selection)
	{
		switch ((GuideSelection) selection)
		{
			case GuideSelection.DesignerBasic:
			case GuideSelection.VehicleBasic:
				GameGuide.ActiveGuide = (GuideSelection) selection;
				SceneManager.LoadScene(Designer);
				break;
			case GuideSelection.Gameplay:
				GameGuide.ActiveGuide = GuideSelection.Gameplay;
				VehicleSelection.SerializedVehicle = GuideVehicle.text;
				SceneManager.LoadScene(GameplayGuide);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(selection), selection, null);
		}
	}

	public void ToLobby()
	{
		SceneManager.LoadScene(MultiplayerLobby);
	}

	public void Quit()
	{
		Application.Quit();
	}
}
}