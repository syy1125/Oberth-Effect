using Syy1125.OberthEffect.Guide;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Syy1125.OberthEffect.MainMenu
{
public class MainMenu : MonoBehaviour
{
	public SceneReference Designer;
	public SceneReference MultiplayerLobby;

	public void ToDesigner()
	{
		SceneManager.LoadScene(Designer);
	}

	public void ToDesignerGuide()
	{
		GameGuide.ActiveGuide = GuideSelection.DesignerBasic;
		SceneManager.LoadScene(Designer);
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