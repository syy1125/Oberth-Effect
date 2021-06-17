using System.Collections.Generic;
using UnityEngine;

namespace Syy1125.OberthEffect.Common
{
public class VehicleResourceDatabase : MonoBehaviour
{
	public static VehicleResourceDatabase Instance { get; private set; }

	public VehicleResource[] VehicleResources;

	private Dictionary<string, VehicleResource> _resourceById;

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

		_resourceById = new Dictionary<string, VehicleResource>();
		foreach (VehicleResource resource in VehicleResources)
		{
			_resourceById.Add(resource.Id, resource);
		}
	}

	public VehicleResource GetResource(string resourceId)
	{
		if (!_resourceById.TryGetValue(resourceId, out VehicleResource resource))
		{
			Debug.LogError($"Unknown resource {resourceId}");
		}

		return resource;
	}
}
}