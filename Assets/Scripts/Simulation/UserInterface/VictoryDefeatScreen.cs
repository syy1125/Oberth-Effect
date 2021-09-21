using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
public class VictoryDefeatScreen : MonoBehaviour
{
	public Text DisplayText;

	public void ShowVictory()
	{
		gameObject.SetActive(true);
		DisplayText.text = "Victory";
		DisplayText.color = Color.yellow;
	}

	public void ShowDefeat()
	{
		gameObject.SetActive(true);
		DisplayText.text = "Defeat";
		DisplayText.color = new Color(0.75f, 0.75f, 0.75f);
	}
}
}