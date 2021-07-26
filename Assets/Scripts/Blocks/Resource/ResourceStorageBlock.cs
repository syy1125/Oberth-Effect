using System;
using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Spec;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Resource
{
public interface IResourceStorageBlockRegistry : IBlockRegistry<ResourceStorageBlock>, IEventSystemHandler
{}

public class ResourceStorageBlock : MonoBehaviour, ITooltipProvider
{
	[NonSerialized]
	public Dictionary<string, float> ResourceCapacity;

	private void OnEnable()
	{
		ExecuteEvents.ExecuteHierarchy<IResourceStorageBlockRegistry>(
			gameObject, null, (handler, _) => handler.RegisterBlock(this)
		);
	}

	private void OnDisable()
	{
		ExecuteEvents.ExecuteHierarchy<IResourceStorageBlockRegistry>(
			gameObject, null, (handler, _) => handler.UnregisterBlock(this)
		);
	}

	public string GetTooltip()
	{
		return "Resource storage capacity\n"
		       + string.Join(
			       "\n",
			       VehicleResourceDatabase.Instance.FormatResourceDict(ResourceCapacity)
				       .Select(line => $"  {line}")
		       );
	}
}
}