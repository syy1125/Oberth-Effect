using System;
using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Common.UserInterface
{
public class SwitchSelect : MonoBehaviour
{
	[Serializable]
	public class ValueChangeEvent : UnityEvent<int>
	{}

	public Button Left;
	public Button Right;
	public RectTransform Content;
	public GameObject OptionPrefab;
	public string[] Options;

	private int _value;

	public int Value
	{
		get => _value;
		set
		{
			if (value != _value)
			{
				_value = value;
				OnValueChanged.Invoke(_value);
			}
		}
	}

	public ValueChangeEvent OnValueChanged;

	private float _contentVelocity;

	private void Awake()
	{
		InstantiateOptions();
	}

	private void InstantiateOptions()
	{
		for (var i = 0; i < Options.Length; i++)
		{
			GameObject go = Instantiate(OptionPrefab, Content);

			RectTransform rectTransform = go.GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(i, 0);
			rectTransform.anchorMax = new Vector2(i + 1, 1);

			go.GetComponent<Text>().text = Options[i];
		}
	}

	private void OnEnable()
	{
		Left.onClick.AddListener(PrevOption);
		Right.onClick.AddListener(NextOption);
	}

	private void OnDisable()
	{
		Left.onClick.RemoveListener(PrevOption);
		Right.onClick.RemoveListener(NextOption);
	}

	private void PrevOption()
	{
		Value = (Value + Options.Length - 1) % Options.Length;
		OnValueChanged.Invoke(Value);
	}

	private void NextOption()
	{
		Value = (Value + 1) % Options.Length;
		OnValueChanged.Invoke(Value);
	}

	public void SetOptions(string[] options)
	{
		Options = options;

		foreach (Transform child in Content)
		{
			Destroy(child.gameObject);
		}

		InstantiateOptions();

		Value = Mathf.Clamp(Value, 0, Options.Length - 1);

		float position = Content.anchorMin.x;
		position = Mathf.Clamp(position, -Options.Length + 1f, 0f);
		Content.anchorMin = new Vector2(position, 0);
		Content.anchorMax = new Vector2(position + 1, 1);
	}

	private void Update()
	{
		float position = Content.anchorMin.x;
		position = Mathf.SmoothDamp(position, -Value, ref _contentVelocity, 0.1f);
		Content.anchorMin = new Vector2(position, 0);
		Content.anchorMax = new Vector2(position + 1, 1);
	}
}
}