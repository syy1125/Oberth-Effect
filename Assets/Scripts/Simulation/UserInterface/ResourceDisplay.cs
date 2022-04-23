using System;
using System.Collections.Generic;
using Syy1125.OberthEffect.Simulation.Construct;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
public class ResourceDisplay : MonoBehaviour
{
	public GameObject ResourceRowPrefab;

	public string[] DisplayResources;

	private List<ResourceDisplayRow> _rows;

	private void Awake()
	{
		_rows = new List<ResourceDisplayRow>();

		foreach (string resourceId in DisplayResources)
		{
			if (!VehicleResourceDatabase.Instance.ContainsId(resourceId))
			{
				Debug.LogError($"Resource {resourceId} does not exist");
				continue;
			}

			var row = Instantiate(ResourceRowPrefab, transform).GetComponent<ResourceDisplayRow>();
			var spec = VehicleResourceDatabase.Instance.GetResourceSpec(resourceId).Spec;

			row.ResourceId = resourceId;
			row.ShortName.text = spec.ShortName;
			row.ShortName.color = spec.GetDisplayColor();
			row.FillBar.color = spec.GetDisplayColor();

			_rows.Add(row);
		}
	}

	private void LateUpdate()
	{
		VehicleResourceManager resourceManager = PlayerVehicleSpawner.Instance.Vehicle == null
			? null
			: PlayerVehicleSpawner.Instance.Vehicle.GetComponent<VehicleResourceManager>();

		foreach (ResourceDisplayRow row in _rows)
		{
			row.UpdateFrom(resourceManager);
		}
	}
}
}