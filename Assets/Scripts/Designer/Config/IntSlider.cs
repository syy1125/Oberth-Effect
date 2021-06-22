using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Designer.Config
{
[Serializable]
public class ValueChangeEvent : UnityEvent<int>
{}

public class IntSlider : MonoBehaviour
{
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
		Slider.onValueChanged.AddListener(SetFloatValue);
		Input.onEndEdit.AddListener(SetStringValue);
	}

	private void OnDisable()
	{
		Slider.onValueChanged.RemoveListener(SetFloatValue);
		Input.onEndEdit.RemoveListener(SetStringValue);
	}

	public void UpdateFromNormalized(float value)
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

	private void SetFloatValue(float value)
	{
		if (_updatingElements) return;

		int result = Mathf.RoundToInt(value);
		UpdateElementsWith(result);
		OnChange.Invoke(result);
	}

	private void SetStringValue(string value)
	{
		if (_updatingElements) return;

		if (int.TryParse(value, out int result))
		{
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