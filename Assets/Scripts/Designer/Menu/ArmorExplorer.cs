using System;
using Syy1125.OberthEffect.Lib.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Designer.Menu
{
public class ArmorExplorer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
	private const string IDLE_TEXT = "<i>Hover over the graph to see expected damage modifier.</i>";
	
	public TMP_Text Output;

	private RectTransform _transform;

	private void Awake()
	{
		_transform = GetComponent<RectTransform>();
	}

	private void Start()
	{
		Output.text = IDLE_TEXT;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{}

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

		float armorPierce = localPoint.x * 10f;
		float armor = localPoint.y * 10f;

		if (armor < 1 || armorPierce < 1)
		{
			Output.text = $"AP={armorPierce:0.00} vs Armor={armor:0.00}: Unused";
		}
		else
		{
			float damageModifier = Mathf.Min(armorPierce / armor, 1f);
			Output.text = $"AP={armorPierce:0.00} vs Armor={armor:0.00}: {damageModifier:#0.00%} damage multiplier";
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		Output.text = IDLE_TEXT;
	}
}
}