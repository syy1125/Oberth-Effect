using UnityEngine;
using UnityEngine.SceneManagement;

namespace Syy1125.OberthEffect.MainMenu
{
public class MainMenu : MonoBehaviour
{
	public void ToDesigner()
	{
		SceneManager.LoadScene("Scenes/Designer");
	}

	public void Quit()
	{
		Application.Quit();
	}
}
}