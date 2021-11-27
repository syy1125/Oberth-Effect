using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Resource
{
public interface IResourceStorageRegistry : IBlockRegistry<ResourceStorage>
{}

public class ResourceStorage : MonoBehaviour, ITooltipProvider
{
	private Dictionary<string, float> _capacity;

	private void OnEnable()
	{
		GetComponentInParent<IResourceStorageRegistry>()?.RegisterBlock(this);
	}

	public void LoadSpec(Dictionary<string, float> spec)
	{
		_capacity = spec;
	}

	private void OnDisable()
	{
		GetComponentInParent<IResourceStorageRegistry>()?.UnregisterBlock(this);
	}

	public IReadOnlyDictionary<string, float> GetCapacity()
	{
		return _capacity;
	}

	public string GetTooltip()
	{
		return "Resource storage capacity\n  "
		       + string.Join(
			       ", ",
			       VehicleResourceDatabase.Instance.FormatResourceDict(_capacity)
		       );
	}
}
}