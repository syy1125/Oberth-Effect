using System.Collections.Generic;
using Syy1125.OberthEffect.Common;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Resource
{
public interface IResourceGeneratorBlockRegistry : IBlockRegistry<ResourceGeneratorBlock>, IEventSystemHandler
{}

public abstract class ResourceGeneratorBlock : MonoBehaviour
{
	private void OnEnable()
	{
		ExecuteEvents.ExecuteHierarchy<IResourceGeneratorBlockRegistry>(
			gameObject, null, (handler, _) => handler.RegisterBlock(this)
		);
	}

	private void OnDisable()
	{
		ExecuteEvents.ExecuteHierarchy<IResourceGeneratorBlockRegistry>(
			gameObject, null, (handler, _) => handler.UnregisterBlock(this)
		);
	}

	/// <remark>
	/// The return value on this should NOT be time-scaled.
	/// </remark>
	public abstract Dictionary<VehicleResource, float> GetGenerationRate();
}
}