using Syy1125.OberthEffect.Components.UserInterface;
using Syy1125.OberthEffect.Init;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.MainMenu.Options
{
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Tooltip))]
public class KeybindButtonGuard : MonoBehaviour
{
	private void Update()
	{
		if (KeybindManager.Instance.Ready)
		{
			GetComponent<Button>().interactable = true;
			GetComponent<Tooltip>().SetTooltip(null);
		}
		else
		{
			GetComponent<Button>().interactable = false;
			GetComponent<Tooltip>().SetTooltip("A keybind save/load operation is currently in progress.");
		}
	}
}
}