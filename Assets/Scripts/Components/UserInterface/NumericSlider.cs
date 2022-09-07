using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Components.UserInterface
{
public class NumericSlider : MonoBehaviour, IScrollHandler
{
	[Serializable]
	public class ValueChangeEvent : UnityEvent<float>
	{}

	public ValueChangeEvent OnChange;

	[Header("Config")]
	public float MinValue;
	public float MaxValue;
	public float StepSize = 0.01f;
	public string NumberFormat = "0.00";

	private Slider _slider;
	private Slider Slider => _slider == null ? _slider = GetComponentInChildren<Slider>() : _slider;
	private InputField _input;
	private InputField Input => _input == null ? _input = GetComponentInChildren<InputField>() : _input;

	private bool _updatingElements;

	private void Awake()
	{
		Slider.wholeNumbers = true;
		Slider.minValue = 0;
		Slider.maxValue = Mathf.RoundToInt((MaxValue - MinValue) / StepSize);
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

	public void SetValue(float value)
	{
		UpdateElementsWith(value);
	}

	private void UpdateElementsWith(float value)
	{
		_updatingElements = true;

		value = Mathf.Clamp(value, MinValue, MaxValue);
		Slider.value = Mathf.Round((value - MinValue) / StepSize);
		Input.text = value.ToString(NumberFormat);

		_updatingElements = false;
	}

	public void OnScroll(PointerEventData eventData)
	{
		float delta = eventData.scrollDelta.y;
		if (!Mathf.Approximately(delta, 0f))
		{
			float currentValue = MinValue + StepSize * Slider.value;

			float step = StepSize;
			if (Keyboard.current.ctrlKey.isPressed)
			{
				step *= 10;
			}

			if (Keyboard.current.shiftKey.isPressed)
			{
				step *= 100;
			}

			if (delta > 0)
			{
				float value = Mathf.Clamp(currentValue + step, MinValue, MaxValue);
				UpdateElementsWith(value);
				OnChange.Invoke(value);
			}
			else // delta < 0
			{
				float value = Mathf.Clamp(currentValue - step, MinValue, MaxValue);
				UpdateElementsWith(value);
				OnChange.Invoke(value);
			}
		}
	}

	#region Event Listeners

	private void UpdateFromSlider(float sliderValue)
	{
		if (_updatingElements) return;

		float value = MinValue + StepSize * sliderValue;
		UpdateElementsWith(value);
		OnChange.Invoke(value);
	}

	private void UpdateFromInputField(string inputValue)
	{
		if (_updatingElements) return;

		if (float.TryParse(inputValue, out float result))
		{
			result = Mathf.Clamp(result, MinValue, MaxValue);
			UpdateElementsWith(result);
			OnChange.Invoke(result);
		}
		else
		{
			UpdateElementsWith(MinValue + StepSize * Slider.value);
		}
	}

	#endregion
}
}