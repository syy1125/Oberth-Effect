using System;
using System.Collections.Generic;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Simulation.Vehicle;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
public class ResourceDisplay : MonoBehaviour
{
	public GameObject ResourceRowPrefab;

	public VehicleResource[] DisplayResources;

	[HideInInspector]
	public VehicleResourceManager ResourceManager;

	private Dictionary<VehicleResource, ResourceDisplayRow> _rows;

	private void Awake()
	{
		_rows = new Dictionary<VehicleResource, ResourceDisplayRow>();

		foreach (VehicleResource resource in DisplayResources)
		{
			var row = Instantiate(ResourceRowPrefab, transform).GetComponent<ResourceDisplayRow>();

			row.ShortName.text = resource.ShortName;
			row.ShortName.color = resource.DisplayColor;
			row.FillBar.color = resource.DisplayColor;

			_rows.Add(resource, row);
		}
	}

	private void LateUpdate()
	{
		if (ResourceManager != null)
		{
			foreach (KeyValuePair<VehicleResource, ResourceDisplayRow> entry in _rows)
			{
				ResourceDisplayRow row = entry.Value;
				Tuple<float, float, float> resourceStatus = ResourceManager.GetResourceStatus(entry.Key);

				if (resourceStatus == null)
				{
					row.FillBar.fillAmount = 0f;
					row.FillPercent.text = "N/A";
					row.WarningIcon.SetActive(false);
				}
				else
				{
					float fillAmount = resourceStatus.Item1 / resourceStatus.Item2;
					row.FillBar.fillAmount = fillAmount;
					row.FillPercent.text = $"{fillAmount * 100:F1}%";
					row.WarningIcon.SetActive(!Mathf.Approximately(resourceStatus.Item3, 1f));
				}
			}
		}
	}
}
}