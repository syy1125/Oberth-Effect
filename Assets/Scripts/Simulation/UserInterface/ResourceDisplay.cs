using System;
using System.Collections.Generic;
using Syy1125.OberthEffect.Simulation.Vehicle;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
public class ResourceDisplay : MonoBehaviour
{
	public GameObject ResourceRowPrefab;

	public string[] DisplayResources;

	[NonSerialized]
	public VehicleResourceManager ResourceManager;

	private Dictionary<string, ResourceDisplayRow> _rows;

	private void Awake()
	{
		_rows = new Dictionary<string, ResourceDisplayRow>();

		foreach (string resourceId in DisplayResources)
		{
			if (!VehicleResourceDatabase.Instance.HasResource(resourceId))
			{
				Debug.LogError($"Resource {resourceId} does not exist");
				continue;
			}

			var row = Instantiate(ResourceRowPrefab, transform).GetComponent<ResourceDisplayRow>();
			var spec = VehicleResourceDatabase.Instance.GetResourceSpec(resourceId).Spec;

			row.ShortName.text = spec.ShortName;
			row.ShortName.color = spec.GetDisplayColor();
			row.FillBar.color = spec.GetDisplayColor();

			_rows.Add(resourceId, row);
		}
	}

	private void LateUpdate()
	{
		if (ResourceManager != null)
		{
			foreach (KeyValuePair<string, ResourceDisplayRow> entry in _rows)
			{
				ResourceDisplayRow row = entry.Value;
				var resourceStatus = ResourceManager.GetResourceStatus(entry.Key);

				if (resourceStatus == null)
				{
					row.FillBar.fillAmount = 0f;
					row.FillPercent.text = "N/A";
					row.WarningIcon.SetActive(false);
				}
				else
				{
					float fillAmount = resourceStatus.CurrentAmount / resourceStatus.StorageCapacity;
					row.FillBar.fillAmount = fillAmount;
					row.FillPercent.text = $"{fillAmount * 100:F1}%";
					row.WarningIcon.SetActive(!Mathf.Approximately(resourceStatus.Satisfaction, 1f));
				}
			}
		}
	}
}
}