using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class BlockPalette : MonoBehaviour
{
	public GameObject BlockButtonPrefab;
	public GameObject[] Blocks;

	public int SelectedIndex { get; private set; }
	
	private void Start()
	{
		SelectedIndex = -1;

		Transform t = transform;

		foreach (GameObject block in Blocks)
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
		if (SelectedIndex < 0) return null;
		return Blocks[SelectedIndex];
	}
}