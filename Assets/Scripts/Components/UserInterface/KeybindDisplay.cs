using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Components.UserInterface
{
[Serializable]
public struct BindingDisplayToken
{
	public InputActionReference TargetAction;
	public int[] BindingIndices;
	public string Separator;
}

[RequireComponent(typeof(Text))]
public class KeybindDisplay : MonoBehaviour
{
	public BindingDisplayToken[] Tokens;
	public string Separator;

	private const InputControlPath.HumanReadableStringOptions PATH_OPTIONS =
		InputControlPath.HumanReadableStringOptions.OmitDevice
		| InputControlPath.HumanReadableStringOptions.UseShortNames;

	private void OnEnable()
	{
		GetComponent<Text>().text = string.Join(
			Separator,
			Tokens.Select(
				token => string.Join(
					token.Separator, token.BindingIndices
						.Select(index => token.TargetAction.action.bindings[index].effectivePath)
						.Where(path => !string.IsNullOrWhiteSpace(path))
						.Select(path => InputControlPath.ToHumanReadableString(path, PATH_OPTIONS).Replace("/", ""))
				)
			)
		);
	}
}
}