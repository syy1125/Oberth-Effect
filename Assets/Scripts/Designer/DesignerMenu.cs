using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DesignerMenu : MonoBehaviour
{
	[Header("Input")]
	public InputActionReference MenuAction;

	[Header("References")]
	public GameObject[] InitialMenuItems;

	private bool _enabled;
	private Stack<IModal> _modals;

	private void Awake()
	{
		_enabled = false;
		_modals = new Stack<IModal>();
	}

	private void OnEnable()
	{
		MenuAction.action.Enable();
		MenuAction.action.performed += ToggleMenu;
	}

	private void OnDisable()
	{
		MenuAction.action.performed -= ToggleMenu;
		MenuAction.action.Disable();
	}

	private void ToggleMenu(InputAction.CallbackContext context)
	{
		if (!_enabled)
		{
			foreach (GameObject item in InitialMenuItems)
			{
				item.SetActive(true);
			}

			_enabled = true;
		}
		else if (_modals.Count > 0)
		{
			CloseTopModal();
		}
		else
		{
			foreach (GameObject item in InitialMenuItems)
			{
				item.SetActive(false);
			}

			_enabled = false;
		}
	}

	public void OpenModal(IModal modal)
	{
		modal.OpenModal();
		_modals.Push(modal);
	}

	public void CloseTopModal()
	{
		_modals.Pop().CloseModal();
	}
}