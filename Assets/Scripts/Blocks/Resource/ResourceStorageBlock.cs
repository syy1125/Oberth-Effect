using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Resource
{
public interface IResourceStorageBlockRegistry : IBlockRegistry<ResourceStorageBlock>, IEventSystemHandler
{}

public class ResourceStorageBlock : MonoBehaviour, ITooltipProvider
{
	private Dictionary<string, float> _capacity;

	private void OnEnable()
	{
		ExecuteEvents.ExecuteHierarchy<IResourceStorageBlockRegistry>(
			gameObject, null, (handler, _) => handler.RegisterBlock(this)
		);
	}

	public void LoadSpec(Dictionary<string, float> spec)
	{
		_capacity = spec;
	}

	private void OnDisable()
	{
		ExecuteEvents.ExecuteHierarchy<IResourceStorageBlockRegistry>(
			gameObject, null, (handler, _) => handler.UnregisterBlock(this)
		);
	}

	public IReadOnlyDictionary<string, float> GetCapacity()
	{
		return _capacity;
	}

	public string GetTooltip()
	{
		return "Resource storage capacity\n"
		       + string.Join(
			       "\n",
			       VehicleResourceDatabase.Instance.FormatResourceDict(_capacity)
				       .Select(line => $"  {line}")
		       );
	}
}
}