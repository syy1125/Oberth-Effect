using System;
using Syy1125.OberthEffect.Lib.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Designer.Menu
{
public class ArmorExplorer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
	private const string IDLE_TEXT = "<i>Hover over the graph to see expected damage modifier.</i>";

	public TMP_Text Output;
	public RectTransform Indicator;

	private RectTransform _transform;

	private void Awake()
	{
		_transform = GetComponent<RectTransform>();
	}

	private void Start()
	{
		Output.text = IDLE_TEXT;
		Indicator.gameObject.SetActive(false);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		Indicator.gameObject.SetActive(true);
	}

	public void OnPointerMove(PointerEventData eventData)
	{
		if (
			!RectTransformUtils.ScreenPointToNormalizedPointInRectangle(
				_transform, eventData.position, Camera.main, out Vector2 localPoint
			)
		)
		{
			Output.text = "Unexpected error";
			return;
		}

		bool ctrlSnap = Keyboard.current.ctrlKey.isPressed;
		float armorPierce = ctrlSnap ? Mathf.Round(localPoint.x * 10f) : Mathf.Round(localPoint.x * 100f) / 10f;
		float armor = ctrlSnap ? Mathf.Round(localPoint.y * 10f) : Mathf.Round(localPoint.y * 100f) / 10f;

		if (ctrlSnap)
		{
			Indicator.gameObject.SetActive(true);
			Indicator.anchorMin = Indicator.anchorMax = new(armorPierce / 10f, armor / 10f);
		}
		else
		{
			Indicator.gameObject.SetActive(false);
		}

		if (armor < 1 || armorPierce < 1)
		{
			Output.text = $"AP={armorPierce:0.0} vs Armor={armor:0.0}: Unused";
		}
		else
		{
			float damageModifier = Mathf.Min(armorPierce / armor, 1f);
			Output.text = $"AP={armorPierce:0.0} vs Armor={armor:0.0}: {damageModifier:#0.##%} damage multiplier";
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		Output.text = IDLE_TEXT;
		Indicator.gameObject.SetActive(false);
	}
}
}