using System;
using Syy1125.OberthEffect.Simulation.Construct;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
public class ResourceDisplayRow : MonoBehaviour
{
	[NonSerialized]
	public string ResourceId;

	[Header("References")]
	public Text ShortName;
	public Image FillBar;
	public Text FillPercent;
	public GameObject WarningIcon;
	public GameObject ErrorIcon;
	public Text EfficiencyDisplay;

	[Header("Configuration")]
	public float DampRate = 4f;
	public float RateEpsilon = 1e-3f;

	private float _displayChangeRate;
	private float _displayEfficiency;

	public void UpdateFrom(VehicleResourceManager resourceManager)
	{
		if (resourceManager == null)
		{
			ShowInvalidState();
			return;
		}

		var resourceStatus = resourceManager.GetResourceStatus(ResourceId);

		if (resourceStatus == null)
		{
			ShowInvalidState();
			return;
		}

		float fillAmount = resourceStatus.CurrentAmount / resourceStatus.StorageCapacity;
		FillBar.fillAmount = fillAmount;
		FillPercent.text = $"{fillAmount * 100:F1}%";

		if (Mathf.Approximately(resourceStatus.Satisfaction, 1f))
		{
			_displayChangeRate = Mathf.Lerp(
				_displayChangeRate, resourceStatus.GenerationRate - resourceStatus.ConsumptionRequestRate,
				DampRate * Time.deltaTime
			);
			_displayEfficiency = 1f;

			if (Mathf.Abs(_displayChangeRate) < RateEpsilon * resourceStatus.StorageCapacity)
			{
				WarningIcon.SetActive(false);
				ErrorIcon.SetActive(false);
				EfficiencyDisplay.text = "";
			}
			else if (_displayChangeRate > 0f)
			{
				float fillTime = (resourceStatus.StorageCapacity - resourceStatus.CurrentAmount) / _displayChangeRate;
				WarningIcon.SetActive(false);
				ErrorIcon.SetActive(false);
				EfficiencyDisplay.text = $"(+{fillTime:F1}s)";
				EfficiencyDisplay.color = Color.white;
			}
			else // _displayChangeRate < 0f
			{
				float emptyTime = -resourceStatus.CurrentAmount / _displayChangeRate;
				WarningIcon.SetActive(emptyTime < 5);
				ErrorIcon.SetActive(false);
				EfficiencyDisplay.text = $"(-{emptyTime:F1}s)";
				EfficiencyDisplay.color = WarningIcon.activeSelf ? new Color(1f, 0.5f, 0f) : Color.white;
			}
		}
		else
		{
			_displayChangeRate = 0f;
			_displayEfficiency = Mathf.Lerp(_displayEfficiency, resourceStatus.Satisfaction, DampRate * Time.deltaTime);

			WarningIcon.SetActive(false);
			ErrorIcon.SetActive(true);
			EfficiencyDisplay.text = $"({_displayEfficiency:0%} eff.)";
			EfficiencyDisplay.color = Color.red;
		}
	}

	private void ShowInvalidState()
	{
		FillBar.fillAmount = 0f;
		FillPercent.text = "N/A";
		WarningIcon.SetActive(false);
		ErrorIcon.SetActive(false);
		EfficiencyDisplay.text = "";
	}
}
}