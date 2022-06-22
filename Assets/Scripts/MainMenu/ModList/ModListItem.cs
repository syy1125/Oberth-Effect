using Syy1125.OberthEffect.Components.UserInterface;
using Syy1125.OberthEffect.Foundation.UserInterface;
using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Spec.ModLoading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.MainMenu.ModList
{
[RequireComponent(typeof(LayoutElement))]
[RequireComponent(typeof(CanvasGroup))]
public class ModListItem : MonoBehaviour,
	IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler,
	IElementControllerContext
{
	public GameObject DropShadowPrefab;
	public Text ModName;
	public Toggle EnableToggle;

	public bool UpdatingElements { get; private set; }

	private RectTransform _transform;
	private LayoutElement _layout;
	private ScrollRect _scroll;
	private VerticalLayoutGroup _layoutGroup;
	private ModList _modList;

	private Vector2 _dragHandle;
	private int _oldIndex;
	private GameObject _dropShadow;

	private void Awake()
	{
		_transform = GetComponent<RectTransform>();
		_layout = GetComponent<LayoutElement>();
		_scroll = GetComponentInParent<ScrollRect>();
		_layoutGroup = GetComponentInParent<VerticalLayoutGroup>();
		_modList = GetComponentInParent<ModList>();
	}

	public void LoadMod(ModListElement element)
	{
		UpdatingElements = true;

		ModName.text = $"{element.Spec.DisplayName} <size=12>v{element.Spec.Version}</size>";
		GetComponent<Tooltip>().SetTooltip(element.Spec.Description);
		EnableToggle.isOn = element.Enabled;
		EnableToggle.onValueChanged.AddListener(SetModEnabled);

		UpdatingElements = false;
	}

	private void SetModEnabled(bool modEnabled)
	{
		_modList.SetModEnabled(transform.GetSiblingIndex(), modEnabled);
	}

	public void OnInitializePotentialDrag(PointerEventData eventData)
	{
		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			_transform, eventData.position, eventData.pressEventCamera, out _dragHandle
		);
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		_oldIndex = transform.GetSiblingIndex();
		_dropShadow = Instantiate(DropShadowPrefab, transform.parent);
		_dropShadow.transform.SetSiblingIndex(_oldIndex);
		transform.SetAsLastSibling();

		_layout.ignoreLayout = true;
		GetComponent<CanvasGroup>().blocksRaycasts = false;
	}

	public void OnDrag(PointerEventData eventData)
	{
		ScrollRectUtils.DragEdgeScroll(_scroll, eventData);

		_dropShadow.transform.SetSiblingIndex(ComputeDropIndex(eventData));

		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			_transform, eventData.position, eventData.pressEventCamera, out Vector2 point
		);
		_transform.Translate(point - _dragHandle, Space.Self);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		int newIndex = ComputeDropIndex(eventData);
		_modList.MoveModIndex(_oldIndex, newIndex);

		Destroy(_dropShadow);
		transform.SetSiblingIndex(newIndex);
		_layout.ignoreLayout = false;
		GetComponent<CanvasGroup>().blocksRaycasts = true;
	}

	private int ComputeDropIndex(PointerEventData pointer)
	{
		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			_scroll.viewport, pointer.position, pointer.pressEventCamera, out Vector2 point
		);
		var rect = _scroll.viewport.rect;
		float offset = Mathf.Clamp(rect.max.y - point.y, 0f, rect.height);
		float unitHeight = _layout.preferredHeight + _layoutGroup.spacing;

		int index = Mathf.RoundToInt((offset - _layoutGroup.padding.top) / unitHeight - 0.5f);
		return Mathf.Clamp(index, 0, _modList.ModCount - 1);
	}
}
}