using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Designer;
using Syy1125.OberthEffect.Designer.Palette;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect
{
public class DesignerPaletteUse : MonoBehaviour
{
	[Header("References")]
	public VehicleDesigner Designer;
	public DesignerAreaMask AreaMask;
	public GameObject EraserIndicator;

	public DesignerGridMove GridMove;
	public BlockPalette Palette;
	public VehicleBuilder Builder;
	public BlockIndicators Indicators;
	public Transform PreviewParent;

	[Header("Input Actions")]
	public InputActionReference RotateAction;
	public InputActionReference UsePaletteAction;

	public IPaletteSelection CurrentSelection => Palette.CurrentSelection;

	// State
	private int _rotation;
	private GameObject _paletteActionPreview;

	private Vector2Int? _hoverPosition;
	private Vector2Int? _prevHover;

	private Vector2Int? _usePosition;
	private Vector2Int? _prevUse;

	private bool _paletteSelectionChanged;

	private void OnEnable()
	{
		RotateAction.action.performed += HandleRotate;

		Palette.OnSelectionChanged += OnPaletteSelectionChanged;

		// Reset internal state
		_paletteSelectionChanged = true;
		_prevHover = null;
		_prevUse = null;
	}

	private void Start()
	{
		EraserIndicator.SetActive(false);
	}

	private void OnDisable()
	{
		RotateAction.action.performed -= HandleRotate;

		Palette.OnSelectionChanged -= OnPaletteSelectionChanged;
	}

	#region Update

	private void Update()
	{
		if (AreaMask.Hovering) _hoverPosition = Designer.HoverPositionInt;
		else _hoverPosition = null;

		bool usePalette = AreaMask.Hovering && !GridMove.Dragging && UsePaletteAction.action.ReadValue<float>() > 0.5f;
		_usePosition = usePalette ? _hoverPosition : null;

		if (_paletteSelectionChanged || _hoverPosition != _prevHover)
		{
			UpdatePreview();
			UpdateConflicts();
		}

		if (_paletteSelectionChanged || _usePosition != _prevUse)
		{
			UpdatePaletteUse();
		}

		_prevHover = _hoverPosition;
		_prevUse = _usePosition;
		_paletteSelectionChanged = false;
	}

	private void UpdatePreview()
	{
		switch (Palette.CurrentSelection)
		{
			case CursorSelection _:
				if (_paletteActionPreview != null)
				{
					Destroy(_paletteActionPreview);
					_paletteActionPreview = null;
				}

				EraserIndicator.SetActive(false);

				break;

			case EraserSelection _:
				if (_paletteActionPreview != null)
				{
					Destroy(_paletteActionPreview);
					_paletteActionPreview = null;
				}

				if (_hoverPosition == null)
				{
					EraserIndicator.SetActive(false);
					break;
				}

				VehicleBlueprint.BlockInstance blockInstance = Builder.GetBlockInstanceAt(_hoverPosition.Value);
				if (blockInstance == null)
				{
					EraserIndicator.SetActive(false);
					break;
				}

				BlockSpec blockSpec = BlockDatabase.Instance.GetSpecInstance(blockInstance.BlockId).Spec;
				BlockBounds blockBounds = new BlockBounds(
					blockSpec.Construction.BoundsMin, blockSpec.Construction.BoundsMax
				);

				Vector2 center = blockInstance.Position + blockBounds.Center - new Vector2(0.5f, 0.5f);
				EraserIndicator.transform.localPosition = center;
				EraserIndicator.GetComponent<SpriteRenderer>().size = blockBounds.Size;

				EraserIndicator.SetActive(true);

				break;

			case BlockSelection blockSelection:
				if (_paletteSelectionChanged)
				{
					if (_paletteActionPreview != null)
					{
						Destroy(_paletteActionPreview);
					}

					_paletteActionPreview = BlockBuilder.BuildFromSpec(
						blockSelection.BlockSpec, PreviewParent, _hoverPosition.GetValueOrDefault(), _rotation
					);
					_paletteActionPreview.SetActive(_hoverPosition.HasValue && !GridMove.Dragging);

					foreach (SpriteRenderer sprite in _paletteActionPreview.GetComponentsInChildren<SpriteRenderer>())
					{
						Color c = sprite.color;
						c.a *= 0.5f;
						sprite.color = c;
					}
				}
				else
				{
					_paletteActionPreview.transform.localPosition = (Vector2) _hoverPosition.GetValueOrDefault();
					_paletteActionPreview.transform.localRotation = TransformUtils.GetPhysicalRotation(_rotation);
					_paletteActionPreview.SetActive(_hoverPosition.HasValue && !GridMove.Dragging);
				}

				EraserIndicator.gameObject.SetActive(false);

				break;
		}
	}

	private void UpdatePaletteUse()
	{
		if (_usePosition == null) return;
		Vector2Int usePosition = _usePosition.Value;

		switch (Palette.CurrentSelection)
		{
			case BlockSelection blockSelection:
				try
				{
					Builder.AddBlock(blockSelection.BlockSpec, usePosition, _rotation);
					UpdateDisconnections();
				}
				catch (DuplicateBlockError error)
				{
					// TODO
				}

				break;
			case EraserSelection _:
				try
				{
					Builder.RemoveBlock(usePosition);
					UpdateDisconnections();
					EraserIndicator.gameObject.SetActive(false);
				}
				catch (EmptyBlockError)
				{
					// TODO
				}
				catch (BlockNotErasable)
				{
					// TODO
				}

				break;
		}
	}

	private void UpdateConflicts()
	{
		if (Palette.CurrentSelection is BlockSelection blockSelection && _hoverPosition != null)
		{
			Indicators.SetConflicts(
				Builder.GetConflicts(blockSelection.BlockSpec, _hoverPosition.Value, _rotation)
			);
		}
		else
		{
			Indicators.SetConflicts(null);
		}
	}

	#endregion

	private void UpdateDisconnections()
	{
		Indicators.SetDisconnections(Builder.GetDisconnectedPositions());
	}

	private void HandleRotate(InputAction.CallbackContext context)
	{
		if (Keyboard.current.leftShiftKey.ReadValue() > 0)
		{
			_rotation = (_rotation + 1) % 4;
		}
		else
		{
			_rotation = (_rotation + 3) % 4;
		}

		if (_paletteActionPreview != null)
		{
			_paletteActionPreview.transform.rotation = Quaternion.AngleAxis(_rotation * 90f, Vector3.forward);
		}
	}

	private void OnPaletteSelectionChanged()
	{
		_paletteSelectionChanged = true;
	}
}
}