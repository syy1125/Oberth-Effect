using System;
using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Spec.Block;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Database
{
public class BlockDatabase : MonoBehaviour
{
	public static BlockDatabase Instance { get; private set; }

	private Dictionary<string, BlockSpec> _specs;

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

		_specs = ModLoader.AllBlocks
			.ToDictionary(instance => instance.Spec.BlockId, instance => instance.Spec);
	}

	public IEnumerable<BlockSpec> ListBlocks()
	{
		return _specs.Values;
	}

	public bool HasBlock(string blockId)
	{
		return _specs.ContainsKey(blockId);
	}

	public BlockSpec GetBlockSpec(string blockId)
	{
		return _specs[blockId];
	}
}
}