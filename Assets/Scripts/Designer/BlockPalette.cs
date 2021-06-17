using System.Linq;
using Syy1125.OberthEffect.Blocks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Designer
{
[RequireComponent(typeof(GridLayoutGroup))]
public class BlockPalette : MonoBehaviour
{
	public const int CURSOR_INDEX = -1;
	public const int ERASE_INDEX = -2;

	public GameObject BlockButtonPrefab;

	public InputActionReference CursorAction;
	public InputActionReference EraserAction;

	private GameObject[] _blocks;

	public int SelectedIndex { get; private set; }
	private int? _storedIndex; // For persisting index across disable/enable

	public delegate void IndexChangeEvent();

	public IndexChangeEvent OnIndexChanged;

	private void OnEnable()
	{
		CursorAction.action.Enable();
		EraserAction.action.Enable();
		CursorAction.action.performed += SelectCursor;
		EraserAction.action.performed += SelectEraser;

		if (_storedIndex != null)
		{
			SelectBlockIndex(_storedIndex.Value);
		}
	}

	private void OnDisable()
	{
		_storedIndex = SelectedIndex;
		SelectCursor(new InputAction.CallbackContext());

		CursorAction.action.performed -= SelectCursor;
		EraserAction.action.performed -= SelectEraser;
		CursorAction.action.Disable();
		EraserAction.action.Disable();
	}

	private void Start()
	{
		SelectedIndex = CURSOR_INDEX;

		Transform t = transform;

		_blocks = BlockDatabase.Instance.Blocks
			.Where(block => block.GetComponent<BlockInfo>().ShowInDesigner)
			.ToArray();

		foreach (GameObject block in _blocks)
		{
			GameObject button = Instantiate(BlockButtonPrefab, t);
			button.GetComponent<BlockButton>().DisplayBlock(block);
		}
	}

	public void SelectBlockIndex(int index)
	{
		if (index == SelectedIndex) return;

		if (SelectedIndex >= 0)
		{
			transform.GetChild(SelectedIndex).GetComponent<BlockButton>().OnDeselect();
		}

		SelectedIndex = index;

		OnIndexChanged?.Invoke();
	}

	private void SelectCursor(InputAction.CallbackContext context)
	{
		SelectBlockIndex(CURSOR_INDEX);
	}

	private void SelectEraser(InputAction.CallbackContext context)
	{
		SelectBlockIndex(ERASE_INDEX);
	}

	public GameObject GetSelectedBlock()
	{
		return SelectedIndex < 0 ? null : _blocks[SelectedIndex];
	}
}
}