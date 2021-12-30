using Syy1125.OberthEffect.Spec;
using Syy1125.OberthEffect.Spec.ModLoading;
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
			$"Oberth Effect v{Application.version} (<color=\"yellow\">{ModLoader.StrictChecksum % 0x10000:x4}</color>)",
			$"Unity {Application.unityVersion}"
		);
	}
}
}