using System;
using System.Collections.Generic;
using Syy1125.OberthEffect.Spec;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Designer.Palette
{
[RequireComponent(typeof(GridLayoutGroup))]
public class BlockPalette : MonoBehaviour
{
	public const int CURSOR_INDEX = -1;
	public const int ERASE_INDEX = -2;

	public GameObject BlockButtonPrefab;

	public InputActionReference CursorAction;
	public InputActionReference EraserAction;

	private Dictionary<string, BlockButton> _buttons;
	public IPaletteSelection Selection { get; private set; }

	private void Awake()
	{
		_buttons = new Dictionary<string, BlockButton>();
	}

	private void OnEnable()
	{
		CursorAction.action.Enable();
		EraserAction.action.Enable();
		CursorAction.action.performed += SelectCursor;
		EraserAction.action.performed += SelectEraser;
	}

	private void Start()
	{
		Transform t = transform;

		foreach (SpecInstance<BlockSpec> instance in BlockDatabase.Instance.ListBlocks())
		{
			GameObject buttonObject = Instantiate(BlockButtonPrefab, t);
			BlockButton button = buttonObject.GetComponent<BlockButton>();

			button.DisplayBlock(instance);

			_buttons.Add(instance.Spec.BlockId, button);
		}

		Selection = CursorSelection.Instance;
	}

	private void OnDisable()
	{
		SelectCursor(new InputAction.CallbackContext());

		CursorAction.action.performed -= SelectCursor;
		EraserAction.action.performed -= SelectEraser;
		CursorAction.action.Disable();
		EraserAction.action.Disable();
	}

	public void SelectBlock(string blockId)
	{
		Selection = new BlockSelection(blockId);
	}

	private void SelectCursor(InputAction.CallbackContext context)
	{
		Selection = CursorSelection.Instance;
	}

	private void SelectEraser(InputAction.CallbackContext context)
	{
		Selection = EraserSelection.Instance;
	}

	public GameObject GetSelectedBlock()
	{
		// TODO
		return null;
	}
}
}