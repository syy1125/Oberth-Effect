using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.ModLoading;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Database
{
public class BlockDatabase : MonoBehaviour, IGameContentDatabase
{
	public static BlockDatabase Instance { get; private set; }

	private Dictionary<string, SpecInstance<BlockSpec>> _specs;
	private Dictionary<string, SpecInstance<BlockCategorySpec>> _categories;
	private List<SpecInstance<BlockCategorySpec>> _categoryList;

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
	}

	public void Reload()
	{
		_specs = ModLoader.BlockPipeline.Results
			.Where(instance => instance.Spec.Enabled)
			.ToDictionary(instance => instance.Spec.BlockId, instance => instance);
		_categories = ModLoader.BlockCategoryPipeline.Results
			.Where(instance => instance.Spec.Enabled)
			.ToDictionary(instance => instance.Spec.BlockCategoryId, instance => instance);
		_categoryList = ModLoader.BlockCategoryPipeline.Results
			.Where(instance => instance.Spec.Enabled)
			.OrderBy(instance => instance.Spec.Order)
			.ToList();
		Debug.Log($"Loaded {_specs.Count} block specs");
		Debug.Log($"Loaded {_categoryList.Count} block categories");
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public IEnumerable<SpecInstance<BlockSpec>> ListBlocks()
	{
		return _specs.Values;
	}

	public bool ContainsId(string blockId)
	{
		return blockId != null && _specs.ContainsKey(blockId);
	}

	public SpecInstance<BlockSpec> GetBlockSpecInstance(string blockId)
	{
		return _specs.TryGetValue(blockId, out SpecInstance<BlockSpec> instance) ? instance : default;
	}

	public BlockSpec GetBlockSpec(string blockId)
	{
		return _specs.TryGetValue(blockId, out SpecInstance<BlockSpec> instance) ? instance.Spec : null;
	}

	public static string GetMirrorBlockId(BlockSpec blockSpec)
	{
		string mirrorBlockId = blockSpec.Construction.MirrorBlockId;
		return string.IsNullOrEmpty(mirrorBlockId) ? blockSpec.BlockId : mirrorBlockId;
	}

	public IEnumerable<SpecInstance<BlockCategorySpec>> ListCategories()
	{
		return _categoryList;
	}

	public BlockCategorySpec GetCategory(string categoryId)
	{
		return _categories.TryGetValue(categoryId, out SpecInstance<BlockCategorySpec> instance) ? instance.Spec : null;
	}
}
}