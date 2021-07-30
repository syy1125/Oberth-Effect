﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Database
{
public class VehicleResourceDatabase : MonoBehaviour
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

		_specs = ModLoader.AllVehicleResources
			.ToDictionary(instance => instance.Spec.ResourceId, instance => instance);
	}

	public bool HasResource(string resourceId)
	{
		return _specs.ContainsKey(resourceId);
	}

	public SpecInstance<VehicleResourceSpec> GetResourceSpec(string resourceId)
	{
		return _specs[resourceId];
	}

	public IEnumerable<string> FormatResourceDict(IReadOnlyDictionary<string, float> dict)
	{
		return dict
			.Where(entry => HasResource(entry.Key))
			.Select(entry => new Tuple<VehicleResourceSpec, float>(GetResourceSpec(entry.Key).Spec, entry.Value))
			.Select(entry => entry.Item1.WrapColorTag($"{entry.Item2:0.#} {entry.Item1.DisplayColor}"));
	}
}
}