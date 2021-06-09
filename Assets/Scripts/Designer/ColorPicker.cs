using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Designer
{
[Serializable]
public class ColorChangeEvent : UnityEvent<Color>
{}

[RequireComponent(typeof(RectTransform))]
public class ColorPicker : MonoBehaviour
{
	public ColorChangeEvent OnChange;

	[Header("Content")]
	public GameObject Content;

	public RectTransform ModeSelectContent;
	public GameObject[] ContentPanels;

	[Header("UI Elements")]
	public Slider VisualSlider;
	public ColorVisualSquare VisualSquare;

	[Space]
	public IntSlider HueSlider;
	public Image HueBackground;
	public IntSlider SaturationSlider;
	public Image SaturationBackground;
	public IntSlider ValueSlider;
	public Image ValueBackground;

	[Space]
	public IntSlider RedSlider;
	public Image RedBackground;
	public IntSlider GreenSlider;
	public Image GreenBackground;
	public IntSlider BlueSlider;
	public Image BlueBackground;

	private RectTransform _transform;

	private bool _expanded;

	private int _mode;

	private float _modeContentPosition;
	private float _modeContentVelocity;

	private Color _color;
	private Vector3 _hsv;

	private void Awake()
	{
		_transform = GetComponent<RectTransform>();
		SetMode(0);
		_modeContentPosition = 0;
		_modeContentVelocity = 0;
	}

	private void OnEnable()
	{
		UnpackColor();
		UpdateElements();

		VisualSlider.onValueChanged.AddListener(SetVisualHue);
		VisualSquare.OnChange.AddListener(SetSaturationAndValue);

		HueSlider.OnChange.AddListener(SetHue);
		SaturationSlider.OnChange.AddListener(SetSaturation);
		ValueSlider.OnChange.AddListener(SetValue);

		RedSlider.OnChange.AddListener(SetRed);
		GreenSlider.OnChange.AddListener(SetGreen);
		BlueSlider.OnChange.AddListener(SetBlue);
	}

	private void OnDisable()
	{
		VisualSlider.onValueChanged.RemoveListener(SetVisualHue);
		VisualSquare.OnChange.RemoveListener(SetSaturationAndValue);

		HueSlider.OnChange.RemoveListener(SetHue);
		SaturationSlider.OnChange.RemoveListener(SetSaturation);
		ValueSlider.OnChange.RemoveListener(SetValue);

		RedSlider.OnChange.RemoveListener(SetRed);
		GreenSlider.OnChange.RemoveListener(SetGreen);
		BlueSlider.OnChange.RemoveListener(SetBlue);
	}

	private void Start()
	{
		Collapse();
	}

	public void ToggleExpansion()
	{
		if (_expanded)
		{
			Collapse();
		}
		else
		{
			Expand();
		}
	}

	private void Expand()
	{
		Content.SetActive(true);

		LayoutRebuilder.ForceRebuildLayoutImmediate(_transform);
		LayoutRebuilder.MarkLayoutForRebuild(GetComponentInParent<LayoutGroup>().GetComponent<RectTransform>());
		_expanded = true;
	}

	private void Collapse()
	{
		Content.SetActive(false);

		LayoutRebuilder.ForceRebuildLayoutImmediate(_transform);
		LayoutRebuilder.MarkLayoutForRebuild(GetComponentInParent<LayoutGroup>().GetComponent<RectTransform>());
		_expanded = false;
	}

	public void PrevMode()
	{
		SetMode(_mode + ContentPanels.Length - 1);
	}

	public void NextMode()
	{
		SetMode(_mode + 1);
	}

	private void SetMode(int mode)
	{
		_mode = mode % ContentPanels.Length;

		for (var i = 0; i < 3; i++)
		{
			ContentPanels[i].SetActive(i == _mode);
		}
	}

	public void InitColor(Color color)
	{
		_color = color;
		UnpackColor();
		UpdateElements();
	}

	private void Update()
	{
		_modeContentPosition = Mathf.SmoothDamp(_modeContentPosition, _mode, ref _modeContentVelocity, 0.1f);
		ModeSelectContent.anchorMin = new Vector2(-_modeContentPosition, 0);
		ModeSelectContent.anchorMax = new Vector2(-_modeContentPosition + 1, 1);
	}

	private void UnpackColor()
	{
		Color.RGBToHSV(_color, out _hsv.x, out _hsv.y, out _hsv.z);
	}

	private void PackColor()
	{
		_color = Color.HSVToRGB(_hsv.x, _hsv.y, _hsv.z);
	}

	private void SetVisualHue(float h)
	{
		_hsv.x = h;
		PackColor();

		EmitColor();
	}

	private void SetSaturationAndValue(Vector2 position)
	{
		_hsv.y = position.x;
		_hsv.z = position.y;
		PackColor();

		EmitColor();
	}

	private void SetHue(int hue)
	{
		_hsv.x = hue / 360f;
		PackColor();

		EmitColor();
	}

	private void SetSaturation(int s)
	{
		_hsv.y = s / 100f;
		PackColor();

		EmitColor();
	}

	private void SetValue(int v)
	{
		_hsv.z = v / 100f;
		PackColor();

		EmitColor();
	}

	private void SetRed(int red)
	{
		_color.r = red / 255f;
		UnpackColor();

		EmitColor();
	}

	private void SetGreen(int green)
	{
		_color.g = green / 255f;
		UnpackColor();

		EmitColor();
	}

	private void SetBlue(int blue)
	{
		_color.b = blue / 255f;
		UnpackColor();

		EmitColor();
	}

	private void EmitColor()
	{
		OnChange.Invoke(_color);
		UpdateElements();
	}

	private void UpdateElements()
	{
		VisualSlider.value = _hsv.x;
		VisualSquare.UpdateColor(_hsv);

		var fakeHsvColor = new Color(_hsv.x, _hsv.y, _hsv.z);
		HueSlider.UpdateFromNormalized(_hsv.x);
		HueBackground.color = fakeHsvColor;
		SaturationSlider.UpdateFromNormalized(_hsv.y);
		SaturationBackground.color = fakeHsvColor;
		ValueSlider.UpdateFromNormalized(_hsv.z);
		ValueBackground.color = fakeHsvColor;

		RedSlider.UpdateFromNormalized(_color.r);
		RedBackground.color = _color;
		GreenSlider.UpdateFromNormalized(_color.g);
		GreenBackground.color = _color;
		BlueSlider.UpdateFromNormalized(_color.b);
		BlueBackground.color = _color;
	}
}
}