using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

	public delegate void IndexChangeEvent();

	public IndexChangeEvent OnIndexChanged;

	private void OnEnable()
	{
		CursorAction.action.performed += SelectCursor;
		EraserAction.action.performed += SelectEraser;
	}

	private void OnDisable()
	{
		CursorAction.action.performed -= SelectCursor;
		CursorAction.action.performed -= SelectEraser;
	}

	private void Start()
	{
		SelectedIndex = CURSOR_INDEX;

		Transform t = transform;

		_blocks = BlockRegistry.Instance.Blocks
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