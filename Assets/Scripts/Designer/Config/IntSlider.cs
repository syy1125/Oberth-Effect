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
	private InputField _input;

	private void Awake()
	{
		_slider = GetComponentInChildren<Slider>();
		_input = GetComponentInChildren<InputField>();
	}

	private void Start()
	{
		_slider.wholeNumbers = true;
	}

	private void OnEnable()
	{
		_slider.onValueChanged.AddListener(SetFloatValue);
		_input.onEndEdit.AddListener(SetStringValue);
	}

	private void OnDisable()
	{
		_slider.onValueChanged.RemoveListener(SetFloatValue);
		_input.onEndEdit.RemoveListener(SetStringValue);
	}

	public void UpdateFromNormalized(float value)
	{
		int scaledValue = Mathf.RoundToInt(Mathf.Lerp(_slider.minValue, _slider.maxValue, value));
		UpdateElementsWith(scaledValue);
	}

	private void UpdateElementsWith(int value)
	{
		value = Mathf.RoundToInt(Mathf.Clamp(value, _slider.minValue, _slider.maxValue));
		_slider.value = value;
		_input.text = value.ToString();
	}

	private void SetFloatValue(float value)
	{
		int result = Mathf.RoundToInt(value);
		UpdateElementsWith(result);
		OnChange.Invoke(result);
	}

	private void SetStringValue(string value)
	{
		if (int.TryParse(value, out int result))
		{
			UpdateElementsWith(result);
			OnChange.Invoke(result);
		}
		else
		{
			UpdateElementsWith(Mathf.RoundToInt(_slider.value));
		}
	}
}
}