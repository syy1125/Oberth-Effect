using System.Collections.Generic;
using Syy1125.OberthEffect.Spec;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Designer.Palette
{
public class BlockPalette : MonoBehaviour
{
	public Transform BlockButtonsParent;
	public GameObject BlockButtonPrefab;

	public InputActionReference CursorAction;
	public InputActionReference EraserAction;

	private Dictionary<string, BlockButton> _buttons;
	private BlockButton _selectedBlockButton;
	public IPaletteSelection CurrentSelection { get; private set; }
	private IPaletteSelection _storedSelection; // Persists selection over enable/disable

	public delegate void SelectionChangeEvent();

	public SelectionChangeEvent OnSelectionChanged;

	private void Awake()
	{
		_buttons = new Dictionary<string, BlockButton>();
	}

	private void OnEnable()
	{
		CursorAction.action.performed += SelectCursor;
		EraserAction.action.performed += SelectEraser;

		switch (_storedSelection)
		{
			case BlockSelection blockSelection:
				SelectBlock(blockSelection.BlockId);
				break;
			case CursorSelection _:
				SelectCursor(new InputAction.CallbackContext());
				break;
			case EraserSelection _:
				SelectCursor(new InputAction.CallbackContext());
				break;
			case null:
				break;
			default:
				Debug.LogError($"Invalid stored selection {_storedSelection}");
				SelectCursor(new InputAction.CallbackContext());
				break;
		}
	}

	private void Start()
	{
		foreach (SpecInstance<BlockSpec> instance in BlockDatabase.Instance.ListBlocks())
		{
			if (!instance.Spec.Construction.ShowInDesigner) continue;

			GameObject buttonObject = Instantiate(BlockButtonPrefab, BlockButtonsParent);
			BlockButton button = buttonObject.GetComponent<BlockButton>();

			button.DisplayBlock(instance);

			_buttons.Add(instance.Spec.BlockId, button);
		}

		SelectCursor(new InputAction.CallbackContext());
	}

	private void OnDisable()
	{
		_storedSelection = CurrentSelection;
		SelectCursor(new InputAction.CallbackContext());

		CursorAction.action.performed -= SelectCursor;
		EraserAction.action.performed -= SelectEraser;
	}

	public void SetSelectedCategory(string category)
	{
		if (string.IsNullOrEmpty(category))
		{
			foreach (BlockButton button in _buttons.Values)
			{
				button.gameObject.SetActive(true);
			}
		}
		else
		{
			foreach (KeyValuePair<string, BlockButton> entry in _buttons)
			{
				BlockSpec blockSpec = BlockDatabase.Instance.GetBlockSpec(entry.Key);
				entry.Value.gameObject.SetActive(blockSpec.CategoryId == category);
			}

			if (CurrentSelection is BlockSelection blockSelection)
			{
				BlockSpec selectedSpec = BlockDatabase.Instance.GetBlockSpec(blockSelection.BlockId);
				if (selectedSpec.CategoryId != category)
				{
					SetSelection(CursorSelection.Instance);
				}
			}
		}
	}

	public void SelectBlock(string blockId)
	{
		SetSelection(new BlockSelection(blockId));
	}

	private void SelectCursor(InputAction.CallbackContext context)
	{
		SetSelection(CursorSelection.Instance);
	}

	private void SelectEraser(InputAction.CallbackContext context)
	{
		SetSelection(EraserSelection.Instance);
	}

	private void SetSelection(IPaletteSelection selection)
	{
		if (CurrentSelection != null && CurrentSelection.Equals(selection)) return;

		CurrentSelection = selection;

		if (_selectedBlockButton != null)
		{
			_selectedBlockButton.OnDeselect();
		}

		if (selection is BlockSelection blockSelection)
		{
			_selectedBlockButton = _buttons[blockSelection.BlockId];
			_selectedBlockButton.OnSelect();
		}
		else
		{
			_selectedBlockButton = null;
		}

		OnSelectionChanged?.Invoke();
	}
}
}