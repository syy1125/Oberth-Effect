using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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