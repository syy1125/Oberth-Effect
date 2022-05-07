using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Foundation.UserInterface
{
[RequireComponent(typeof(InputField))]
public class BlockingInputField : MonoBehaviour, ISelectHandler, IDeselectHandler
{
	public void OnSelect(BaseEventData eventData)
	{
		InputSystem.DisableDevice(Keyboard.current);
	}

	public void OnDeselect(BaseEventData eventData)
	{
		InputSystem.EnableDevice(Keyboard.current);
	}
}
}