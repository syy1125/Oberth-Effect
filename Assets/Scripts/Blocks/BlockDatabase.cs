using System.Collections.Generic;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
public class BlockDatabase : MonoBehaviour
{
	public static BlockDatabase Instance { get; private set; }

	public GameObject[] Blocks;

	private Dictionary<string, GameObject> _blockById;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else if (Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		_blockById = new Dictionary<string, GameObject>();
		foreach (GameObject block in Blocks)
		{
			var info = block.GetComponent<BlockInfo>();
			_blockById.Add(info.BlockID, block);
		}
	}

	public GameObject GetBlock(string blockId)
	{
		if (!_blockById.TryGetValue(blockId, out GameObject block))
		{
			Debug.LogError($"Unknown block {blockId}");
		}

		return block;
	}
}
}