using Syy1125.OberthEffect.Common.Init;
using Syy1125.OberthEffect.Components.UserInterface;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Designer.Menu
{
public class ControlGroupKeybindDisplay : MonoBehaviour
{
	public GameObject DisplayPrefab;
	public Transform Parent;

	private void Start()
	{
		foreach (var controlGroup in ControlGroupDatabase.Instance.ListControlGroups())
		{
			string path = KeybindManager.Instance.GetControlGroupPath(controlGroup.Spec.ControlGroupId);

			if (string.IsNullOrWhiteSpace(path)) continue;

			GameObject row = Instantiate(DisplayPrefab, Parent);
			Text[] texts = row.GetComponentsInChildren<Text>();

			texts[0].text = InputControlPath.ToHumanReadableString(path, KeybindDisplay.PATH_OPTIONS);
			texts[1].text = controlGroup.Spec.KeybindDescription;
		}
	}
}
}