using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Common.UserInterface
{
public class PercentageSlider : MonoBehaviour
{
	[Header("References")]
	public Slider Slider;
	public Text ValueDisplay;

	[Header("Config")]
	public int MinPercent;
	public int MaxPercent;
	public int StepSize;

	public float Value
	{
		get => (Slider.value * StepSize + MinPercent) / 100f;
		set
		{
			Slider.value = (value * 100f - MinPercent) / StepSize;
			ValueDisplay.text = value.ToString("P0");
		}
	}

	public UnityEvent<float> OnChange;

	private void Awake()
	{
		Slider.wholeNumbers = true;
		Slider.minValue = 0;
		Slider.maxValue = (MaxPercent - MinPercent) / (float) StepSize;
	}

	private void OnEnable()
	{
		Slider.onValueChanged.AddListener(HandleValueChange);
	}

	private void OnDisable()
	{
		Slider.onValueChanged.RemoveListener(HandleValueChange);
	}

	private void HandleValueChange(float value)
	{
		float percent = (value * StepSize + MinPercent) / 100f;
		ValueDisplay.text = percent.ToString("P0");
		OnChange.Invoke(percent);
	}
}
}