using System.Text;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Designer.Menu
{
public class ControlGroupKeybindDisplay : MonoBehaviour
{
	public Text AppendKeybind;
	public Text AppendDescription;

	private void Start()
	{
		StringBuilder keybindBuilder = new StringBuilder(AppendKeybind.text);
		StringBuilder descriptionBuilder = new StringBuilder(AppendDescription.text);

		foreach (var controlGroup in ControlGroupDatabase.Instance.ListControlGroups())
		{
			InputAction action = new InputAction(
				controlGroup.Spec.ControlGroupId, InputActionType.Button, controlGroup.Spec.DefaultKeybind
			);

			keybindBuilder
				.Append('\n')
				.Append(action.GetBindingDisplayString().ToUpper());
			descriptionBuilder
				.Append('\n')
				.Append(controlGroup.Spec.KeybindDescription);
		}

		AppendKeybind.text = keybindBuilder.ToString();
		AppendDescription.text = descriptionBuilder.ToString();
	}
}
}