using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Spec.ModLoading;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Database
{
public class StockVehicleDatabase : MonoBehaviour, IGameContentDatabase
{
	public static StockVehicleDatabase Instance { get; private set; }

	private Dictionary<string, SpecInstance<StockVehicleSpec>> _specs;
	private List<string> _vehiclePaths;

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
		_specs = ModLoader.StockVehiclePipeline.Results
			.Where(instance => instance.Spec.Enabled)
			.ToDictionary(instance => instance.Spec.VehicleId, instance => instance);
		_vehiclePaths = _specs.Values.Select(instance => instance.Spec.VehiclePath).ToList();
		_vehiclePaths.Sort();
		Debug.Log($"Loaded {_specs.Count} stock vehicles");
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public bool ContainsId(string id)
	{
		return _specs.ContainsKey(id);
	}

	public IReadOnlyList<string> ListStockVehicles()
	{
		return _vehiclePaths;
	}
}
}