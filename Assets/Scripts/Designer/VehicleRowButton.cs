using Syy1125.OberthEffect.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Designer
{
[RequireComponent(typeof(Button))]
public class VehicleRowButton : MonoBehaviour
{
	[Header("References")]
	public Text NameText;

	private VehicleLoadSave _loadSave;
	private Button _button;
	private int _index;

	private void Awake()
	{
		_loadSave = GetComponentInParent<VehicleLoadSave>();
		_button = GetComponent<Button>();
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

	public void DisplayVehicle(VehicleBlueprint blueprint)
	{
		NameText.text = blueprint.Name;
	}

	public void SetIndex(int index)
	{
		_index = index;
	}

	private void HandleClick()
	{
		_loadSave.SelectIndex(_index);
	}

	public void SetSelected(bool selected)
	{
		if (selected)
		{
			NameText.fontStyle = FontStyle.BoldAndItalic;
			NameText.color = Color.cyan;
		}
		else
		{
			NameText.fontStyle = FontStyle.Normal;
			NameText.color = Color.white;
		}
	}
}
}