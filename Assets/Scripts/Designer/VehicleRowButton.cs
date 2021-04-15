using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class VehicleRowButton : MonoBehaviour
{
	private VehicleLoadSave _loadSave;
	private Button _button;
	private Text _text;

	private void Awake()
	{
		_loadSave = GetComponentInParent<VehicleLoadSave>();
		_button = GetComponent<Button>();
		_text = GetComponentInChildren<Text>();
	}

	private void OnEnable()
	{
		_button.onClick.AddListener(HandleClick);
	}

	private void OnDisable()
	{
		if (_button != null)
		{
			_button.onClick.RemoveListener(HandleClick);
		}
	}

	private void HandleClick()
	{
		_loadSave.SelectIndex(transform.GetSiblingIndex());
	}

	public void Deselect()
	{
		_text.fontStyle = FontStyle.Normal;
		_text.color = Color.white;
	}

	public void OnSelected()
	{
		_text.fontStyle = FontStyle.BoldAndItalic;
		_text.color = Color.cyan;
	}
}