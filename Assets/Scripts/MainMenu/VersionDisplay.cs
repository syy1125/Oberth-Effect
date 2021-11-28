using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.MainMenu
{
[RequireComponent(typeof(Text))]
public class VersionDisplay : MonoBehaviour
{
	private void Start()
	{
		GetComponent<Text>().text = string.Join(
			"\n",
			$"Oberth Effect v{Application.version}",
			$"Unity {Application.unityVersion}"
		);
	}
}
}