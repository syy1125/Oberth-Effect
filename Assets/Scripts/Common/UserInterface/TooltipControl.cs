using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Common.UserInterface
{
[RequireComponent(typeof(RectTransform))]
public class TooltipControl : MonoBehaviour
{
	public enum AnchorCorner
	{
		UpperRight,
		LowerRight,
		LowerLeft,
		UpperLeft
	}

	public static TooltipControl Instance { get; private set; }

	[Header("References")]
	public RectTransform TooltipDisplay;
	public Text TooltipText;

	[Header("Config")]
	public float MaxTooltipWidth;
	public float TooltipOffset;
	public float ReservedEdgeSpace;
	public AnchorCorner[] CornerPreference;

	private RectTransform _transform;
	private Vector2 _textSizeDelta;
	private Vector2 _mouseLocalPosition;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else if (Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		_transform = GetComponent<RectTransform>();
		_textSizeDelta = TooltipText.GetComponent<RectTransform>().sizeDelta;
	}

	private void Start()
	{
		SetTooltip(null);
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void SetTooltip(string content)
	{
		if (string.IsNullOrWhiteSpace(content))
		{
			TooltipDisplay.gameObject.SetActive(false);
		}
		else
		{
			TooltipDisplay.gameObject.SetActive(true);
			TooltipText.text = content;
		}
	}

	private void LateUpdate()
	{
		GetMousePosition();

		Rect rect = _transform.rect;
		Vector2 anchor = new Vector2(
			_mouseLocalPosition.x / rect.width + 0.5f, _mouseLocalPosition.y / rect.height + 0.5f
		);
		TooltipDisplay.anchorMin = TooltipDisplay.anchorMax = anchor;

		bool success = false;
		foreach (AnchorCorner corner in CornerPreference)
		{
			success = UseLayout(rect, corner);
			if (success) break;
		}

		if (!success)
		{
			AnchorCorner bestCorner = _mouseLocalPosition.x > 0
				? _mouseLocalPosition.y > 0
					? AnchorCorner.UpperRight
					: AnchorCorner.LowerRight
				: _mouseLocalPosition.y > 0
					? AnchorCorner.UpperLeft
					: AnchorCorner.LowerLeft;

			UseLayout(rect, bestCorner, true);
		}
	}

	private void GetMousePosition()
	{
		Vector2 mousePosition = Mouse.current.position.ReadValue();
		bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
			_transform, mousePosition, null, out _mouseLocalPosition
		);

		if (!success)
		{
			Debug.LogError($"Screen to local point for mouse at {mousePosition} did not hit canvas");
		}
	}

	private bool UseLayout(Rect rect, AnchorCorner corner, bool force = false)
	{
		Vector2 availableSpace = GetAvailableSpace(rect, corner, _mouseLocalPosition);
		float tooltipWidth = Mathf.Min(
			TooltipText.preferredWidth - _textSizeDelta.x,
			availableSpace.x, MaxTooltipWidth
		);

		// Here we don't actually care about positioning, only that it gets the correct width
		// so that the text component can figure out the correct height
		TooltipDisplay.offsetMin = Vector2.zero;
		TooltipDisplay.offsetMax = new Vector2(tooltipWidth, 0f);

		LayoutRebuilder.ForceRebuildLayoutImmediate(TooltipDisplay);
		float tooltipHeight = TooltipText.preferredHeight - _textSizeDelta.y;

		Vector2 offset = Vector2.zero;
		if (tooltipHeight > availableSpace.y)
		{
			if (force)
			{
				offset = new Vector2(
					0f, corner switch
					{
						AnchorCorner.UpperRight => tooltipHeight - availableSpace.y,
						AnchorCorner.LowerRight => availableSpace.y - tooltipHeight,
						AnchorCorner.LowerLeft => availableSpace.y - tooltipHeight,
						AnchorCorner.UpperLeft => tooltipHeight - availableSpace.y,
						_ => throw new ArgumentOutOfRangeException(nameof(corner), corner, null)
					}
				);
			}
			else
			{
				return false;
			}
		}

		PositionTooltip(corner, new Vector2(tooltipWidth, tooltipHeight), offset);

		return true;
	}

	private void PositionTooltip(AnchorCorner corner, Vector2 size, Vector2 offset)
	{
		switch (corner)
		{
			case AnchorCorner.UpperRight:
				TooltipDisplay.pivot = Vector2.one;
				TooltipDisplay.offsetMin = new Vector2(-size.x - TooltipOffset, -size.y - TooltipOffset) + offset;
				TooltipDisplay.offsetMax = new Vector2(-TooltipOffset, -TooltipOffset) + offset;
				break;
			case AnchorCorner.LowerRight:
				TooltipDisplay.pivot = Vector2.right;
				TooltipDisplay.offsetMin = new Vector2(-size.x - TooltipOffset, TooltipOffset) + offset;
				TooltipDisplay.offsetMax = new Vector2(-TooltipOffset, size.y + TooltipOffset) + offset;
				break;
			case AnchorCorner.LowerLeft:
				TooltipDisplay.pivot = Vector2.zero;
				TooltipDisplay.offsetMin = new Vector2(TooltipOffset, TooltipOffset) + offset;
				TooltipDisplay.offsetMax = new Vector2(size.x + TooltipOffset, size.y + TooltipOffset) + offset;
				break;
			case AnchorCorner.UpperLeft:
				TooltipDisplay.pivot = Vector2.up;
				TooltipDisplay.offsetMin = new Vector2(TooltipOffset, -size.y - TooltipOffset) + offset;
				TooltipDisplay.offsetMax = new Vector2(size.x + TooltipOffset, -TooltipOffset) + offset;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(corner), corner, null);
		}
	}

	private Vector2 GetAvailableSpace(Rect rect, AnchorCorner corner, Vector2 anchor)
	{
		float reservedSpace = TooltipOffset + ReservedEdgeSpace;

		return corner switch
		{
			AnchorCorner.UpperRight => new Vector2(
				anchor.x - rect.xMin - reservedSpace, anchor.y - rect.yMin - reservedSpace
			),
			AnchorCorner.LowerRight => new Vector2(
				anchor.x - rect.xMin - reservedSpace, rect.yMax - anchor.y - reservedSpace
			),
			AnchorCorner.LowerLeft => new Vector2(
				rect.xMax - anchor.x - reservedSpace, rect.yMax - anchor.y - reservedSpace
			),
			AnchorCorner.UpperLeft => new Vector2(
				rect.xMax - anchor.x - reservedSpace, anchor.y - rect.yMin - reservedSpace
			),
			_ => throw new ArgumentOutOfRangeException(nameof(corner), corner, null)
		};
	}
}
}