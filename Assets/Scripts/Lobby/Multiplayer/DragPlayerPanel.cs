using Photon.Pun;
using Syy1125.OberthEffect.Lib.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Lobby.Multiplayer
{
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(LayoutElement))]
[RequireComponent(typeof(PlayerPanel))]
public class DragPlayerPanel : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler,
	IEndDragHandler
{
	public Texture2D GrabTexture;

	private RectTransform _transform;
	private ScrollRect _scroll;
	private Vector2 _dragHandle;

	private void Awake()
	{
		_transform = GetComponent<RectTransform>();
		_scroll = GetComponentInParent<ScrollRect>();
	}

	public void UpdateDraggable()
	{
		enabled = PhotonNetwork.LocalPlayer.IsMasterClient || GetComponent<PlayerPanel>().Player.IsLocal;
	}

	public void OnInitializePotentialDrag(PointerEventData eventData)
	{
		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			_transform, eventData.position, eventData.pressEventCamera, out _dragHandle
		);
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		GetComponent<LayoutElement>().ignoreLayout = true;
		GetComponent<CanvasGroup>().blocksRaycasts = false;
		Cursor.SetCursor(GrabTexture, new Vector2(50f, 50f), CursorMode.Auto);
		transform.localRotation = Quaternion.AngleAxis(2f, Vector3.back);
	}

	public void OnDrag(PointerEventData eventData)
	{
		ScrollRectUtils.DragEdgeScroll(_scroll, eventData);

		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			_transform, eventData.position, eventData.pressEventCamera, out Vector2 point
		);
		_transform.Translate(point - _dragHandle, Space.Self);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		GetComponent<LayoutElement>().ignoreLayout = false;
		GetComponent<CanvasGroup>().blocksRaycasts = true;
		Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
		transform.localRotation = Quaternion.identity;
	}
}
}