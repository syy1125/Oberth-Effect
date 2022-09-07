using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Components.UserInterface
{
public class IntSlider : MonoBehaviour
{
	[Serializable]
	public class ValueChangeEvent : UnityEvent<int>
	{}

	public ValueChangeEvent OnChange;

	private Slider _slider;
	private Slider Slider => _slider == null ? _slider = GetComponentInChildren<Slider>() : _slider;
	private InputField _input;
	private InputField Input => _input == null ? _input = GetComponentInChildren<InputField>() : _input;

	private bool _updatingElements;

	private void Start()
	{
		Slider.wholeNumbers = true;
	}

	private void OnEnable()
	{
		Slider.onValueChanged.AddListener(UpdateFromSlider);
		Input.onEndEdit.AddListener(UpdateFromInputField);
	}

	private void OnDisable()
	{
		Slider.onValueChanged.RemoveListener(UpdateFromSlider);
		Input.onEndEdit.RemoveListener(UpdateFromInputField);
	}

	public void SetFromNormalized(float value)
	{
		int scaledValue = Mathf.RoundToInt(Mathf.Lerp(Slider.minValue, Slider.maxValue, value));
		UpdateElementsWith(scaledValue);
	}

	private void UpdateElementsWith(int value)
	{
		_updatingElements = true;

		value = Mathf.RoundToInt(Mathf.Clamp(value, Slider.minValue, Slider.maxValue));
		Slider.value = value;
		Input.text = value.ToString();

		_updatingElements = false;
	}

	#region Event Listeners

	private void UpdateFromSlider(float value)
	{
		if (_updatingElements) return;

		int result = Mathf.RoundToInt(value);
		UpdateElementsWith(result);
		OnChange.Invoke(result);
	}

	private void UpdateFromInputField(string value)
	{
		if (_updatingElements) return;

		if (int.TryParse(value, out int result))
		{
			result = Mathf.RoundToInt(Mathf.Clamp(result, Slider.minValue, Slider.maxValue));
			UpdateElementsWith(result);
			OnChange.Invoke(result);
		}
		else
		{
			UpdateElementsWith(Mathf.RoundToInt(Slider.value));
		}
	}

	#endregion
}
}