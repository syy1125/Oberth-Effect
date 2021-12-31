using System;
using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Spec.ModLoading;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Database
{
public class VehicleResourceDatabase : MonoBehaviour, IGameContentDatabase
{
	public static VehicleResourceDatabase Instance { get; private set; }
	private Dictionary<string, SpecInstance<VehicleResourceSpec>> _specs;

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
		_specs = ModLoader.VehicleResourcePipeline.Results
			.ToDictionary(instance => instance.Spec.ResourceId, instance => instance);
		Debug.Log($"Loaded {_specs.Count} vehicle resource specs");
	}

	public bool ContainsId(string resourceId)
	{
		return resourceId != null && _specs.ContainsKey(resourceId);
	}

	public SpecInstance<VehicleResourceSpec> GetResourceSpec(string resourceId)
	{
		return _specs[resourceId];
	}

	public IEnumerable<string> FormatResourceDict(IReadOnlyDictionary<string, float> dict)
	{
		return dict
			.Where(entry => ContainsId(entry.Key))
			.Select(entry => new Tuple<VehicleResourceSpec, float>(GetResourceSpec(entry.Key).Spec, entry.Value))
			.Select(entry => entry.Item1.WrapColorTag($"{entry.Item2:0.###} {entry.Item1.DisplayName}"));
	}
}
}