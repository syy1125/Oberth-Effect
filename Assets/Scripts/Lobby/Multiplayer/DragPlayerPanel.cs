using Photon.Pun;
using Syy1125.OberthEffect.Lib.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace Syy1125.OberthEffect.Lobby.Multiplayer
{
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(LayoutElement))]
[RequireComponent(typeof(PlayerPanel))]
public class DragPlayerPanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	public Texture2D GrabTexture;

	private RectTransform _transform;
	private ScrollRect _scroll;
	private Vector2 _handle;

	private void Awake()
	{
		_transform = GetComponent<RectTransform>();
		_scroll = GetComponentInParent<ScrollRect>();
	}

	public void UpdateDraggable()
	{
		enabled = PhotonNetwork.LocalPlayer.IsMasterClient || GetComponent<PlayerPanel>().Player.IsLocal;
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			_transform, eventData.position, eventData.pressEventCamera, out _handle
		);

		GetComponent<LayoutElement>().ignoreLayout = true;
		GetComponent<CanvasGroup>().blocksRaycasts = false;
		Cursor.SetCursor(GrabTexture, new Vector2(50f, 50f), CursorMode.Auto);
		transform.localRotation = Quaternion.AngleAxis(2f, Vector3.back);
	}

	public void OnDrag(PointerEventData eventData)
	{
		float viewportHeight = _scroll.viewport.rect.height;
		RectTransformUtils.ScreenPointToNormalizedPointInRectangle(
			_scroll.viewport, eventData.position, eventData.pressEventCamera, out Vector2 contentPoint
		);

		if (contentPoint.y > 0.95f)
		{
			_scroll.velocity = new Vector2(
				0f, MathUtils.Remap(contentPoint.y, 0.95f, 1f, -0.5f, -0.1f) * viewportHeight
			);
		}
		else if (contentPoint.y < 0.05f)
		{
			_scroll.velocity = new Vector2(0f, MathUtils.Remap(contentPoint.y, 0f, 0.05f, 0.1f, 0.5f) * viewportHeight);
		}
		else
		{
			_scroll.velocity = Vector2.zero;
		}

		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			_transform, eventData.position, eventData.pressEventCamera, out Vector2 point
		);
		_transform.Translate(point - _handle, Space.Self);
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