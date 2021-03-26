using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class BlockPalette : MonoBehaviour
{
	public const int DESELECT_INDEX = -1;
	public const int ERASE_INDEX = -2;

	public GameObject BlockButtonPrefab;
	private GameObject[] _blocks;

	public int SelectedIndex { get; private set; }

	private void Start()
	{
		SelectedIndex = DESELECT_INDEX;

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
	}

	public GameObject GetSelectedBlock()
	{
		return SelectedIndex < 0 ? null : _blocks[SelectedIndex];
	}
}