﻿using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.ModLoading;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Database
{
public class ControlGroupDatabase : MonoBehaviour, IGameContentDatabase
{
	public static ControlGroupDatabase Instance { get; private set; }

	private Dictionary<string, SpecInstance<ControlGroupSpec>> _specs;

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
		_specs = ModLoader.ControlGroupPipeline.Results.ToDictionary(
			instance => instance.Spec.ControlGroupId, instance => instance
		);
		Debug.Log($"Loaded {_specs.Count} control group specs");
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public IEnumerable<SpecInstance<ControlGroupSpec>> ListControlGroups()
	{
		return _specs.Values;
	}

	public bool ContainsId(string id)
	{
		return id != null && _specs.ContainsKey(id);
	}

	public SpecInstance<ControlGroupSpec> GetSpecInstance(string controlGroupId)
	{
		return _specs[controlGroupId];
	}
}
}