using System.Collections.Generic;
using Syy1125.OberthEffect.Common;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Resource
{
public interface IResourceStorageBlockRegistry : IBlockRegistry<ResourceStorageBlock>, IEventSystemHandler
{}

public class ResourceStorageBlock : MonoBehaviour
{
	public ResourceEntry[] ResourceCapacities;

	public Dictionary<VehicleResource, float> ResourceCapacityDict { get; private set; }

	private void OnEnable()
	{
		ExecuteEvents.ExecuteHierarchy<IResourceStorageBlockRegistry>(
			gameObject, null, (handler, _) => handler.RegisterBlock(this)
		);
	}

	private void Start()
	{
		ResourceCapacityDict = new Dictionary<VehicleResource, float>(ResourceCapacities.Length);
		foreach (ResourceEntry entry in ResourceCapacities)
		{
			ResourceCapacityDict.Add(entry.Resource, entry.Amount);
		}
	}

	private void OnDisable()
	{
		ExecuteEvents.ExecuteHierarchy<IResourceStorageBlockRegistry>(
			gameObject, null, (handler, _) => handler.UnregisterBlock(this)
		);
	}
}
}