﻿using System.Collections.Generic;
using UnityEngine;

public class BlockRegistry : MonoBehaviour
{
	public static BlockRegistry Instance { get; private set; }

	public GameObject[] Blocks;

	private Dictionary<string, GameObject> _blockById;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
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
		return _blockById.TryGetValue(blockId, out GameObject block) ? block : null;
	}
}