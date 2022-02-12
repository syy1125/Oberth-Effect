using Syy1125.OberthEffect.Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Common.UserInterface
{
[RequireComponent(typeof(Button))]
public class VehicleRowButton : MonoBehaviour
{
	[Header("References")]
	public Text NameText;
	public Text CostText;

	private VehicleList _vehicleList;
	private Button _button;
	private int _index;

	private void Awake()
	{
		_vehicleList = GetComponentInParent<VehicleList>();
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

	public void DisplayVehicle(VehicleBlueprint blueprint, bool isStock = false)
	{
		NameText.text = isStock ? "[S] " + blueprint.Name : blueprint.Name;
		CostText.text = blueprint.CachedCost.ToString();
	}

	public void SetIndex(int index)
	{
		_index = index;
	}

	public void SetCostColor(Color color)
	{
		CostText.color = color;
	}

	private void HandleClick()
	{
		_vehicleList.SelectIndex(_index);
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