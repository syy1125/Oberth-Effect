using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Components.UserInterface;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Designer.Palette
{
public class DesignerPaletteUse : MonoBehaviour
{
	[Header("References")]
	public VehicleDesigner Designer;
	public VehicleMirror Mirror;
	public DesignerAreaMask AreaMask;
	public FlytextManager FlytextManager;
	public GameObject EraserIndicator;
	public GameObject MirrorEraserIndicator;

	public DesignerGridMove GridMove;
	public BlockPalette Palette;
	public VehicleBuilder Builder;
	public BlockIndicators Indicators;
	public Transform PreviewParent;

	[Header("Input Actions")]
	public InputActionReference RotateAction;
	public InputActionReference InverseRotateAction;
	public InputActionReference UsePaletteAction;
	public InputActionReference ReplacementModeAction;

	public IPaletteSelection CurrentSelection => Palette.CurrentSelection;

	// State
	private int _rotation;
	private int _prevRotation;

	private Vector2Int? _hoverPosition;
	private Vector2Int? _prevHover;

	private int? _mirrorPosition;
	private int? _prevMirror;

	private Vector2Int? _usePosition;
	private Vector2Int? _prevUse;

	private bool _selectionChanged;

	// Owned game objects
	// Block previews stay the same when the player is moving the mouse around.
	// The previews get destroyed and recreated when player selects a different block or enter cursor/eraser mode.
	private GameObject _blockPreview;
	private GameObject _mirrorBlockPreview;

	private void OnEnable()
	{
		RotateAction.action.performed += HandleRotate;

		Palette.OnSelectionChanged += OnPaletteSelectionChanged;

		// Reset internal state
		_selectionChanged = true;
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
		LoadState();

		if (_selectionChanged
		    || _mirrorPosition != _prevMirror
		    || _hoverPosition != _prevHover
		    || _rotation != _prevRotation)
		{
			UpdatePreview();
			UpdateConflicts();
		}

		if (_selectionChanged
		    || _mirrorPosition != _prevMirror
		    || _usePosition != _prevUse)
		{
			UpdatePaletteUse();
		}

		UpdatePrevState();
	}

	private void LoadState()
	{
		if (AreaMask.Hovering) _hoverPosition = Designer.HoverPositionInt;
		else _hoverPosition = null;

		bool usePalette = AreaMask.Hovering && !GridMove.Dragging && UsePaletteAction.action.ReadValue<float>() > 0.5f;
		_usePosition = usePalette ? _hoverPosition : null;

		if (Mirror.UseMirror) _mirrorPosition = Mirror.MirrorPosition;
		else _mirrorPosition = null;
	}

	private void UpdatePrevState()
	{
		_prevHover = _hoverPosition;
		_prevMirror = _mirrorPosition;
		_prevRotation = _rotation;
		_prevUse = _usePosition;
		_selectionChanged = false;
	}

	private void UpdatePreview()
	{
		switch (Palette.CurrentSelection)
		{
			case CursorSelection _:
			{
				DestroyBlockPreview();

				EraserIndicator.SetActive(false);
				MirrorEraserIndicator.SetActive(false);

				break;
			}
			case EraserSelection _:
			{
				DestroyBlockPreview();

				if (_hoverPosition == null)
				{
					EraserIndicator.SetActive(false);
					MirrorEraserIndicator.SetActive(false);
					break;
				}

				Vector2Int hoverPosition = _hoverPosition.Value;

				UpdateEraserIndicator(hoverPosition, EraserIndicator);

				if (_mirrorPosition != null)
				{
					Vector2Int mirrorHoverPosition = new Vector2Int(
						_mirrorPosition.Value - hoverPosition.x, hoverPosition.y
					);
					UpdateEraserIndicator(mirrorHoverPosition, MirrorEraserIndicator);
				}
				else
				{
					MirrorEraserIndicator.SetActive(false);
				}

				break;
			}
			case BlockSelection blockSelection:
			{
				if (_selectionChanged)
				{
					DestroyBlockPreview();
					_blockPreview = CreatePreview(blockSelection.BlockSpec);

					string mirrorBlockId = BlockDatabase.GetMirrorBlockId(blockSelection.BlockSpec);
					_mirrorBlockPreview = CreatePreview(BlockDatabase.Instance.GetBlockSpec(mirrorBlockId));
				}

				if (_hoverPosition != null)
				{
					_blockPreview.transform.localPosition = (Vector2) _hoverPosition.Value;
					_blockPreview.transform.localRotation = TransformUtils.GetPhysicalRotation(_rotation);
					_blockPreview.SetActive(!GridMove.Dragging);
				}
				else
				{
					_blockPreview.SetActive(false);
				}

				if (_hoverPosition != null && _mirrorPosition != null)
				{
					GetMirrorBlock(
						blockSelection.BlockSpec, _hoverPosition.Value, _rotation,
						_mirrorPosition.Value,
						out BlockSpec _, out Vector2Int mirrorRootPosition, out int mirrorRotation
					);

					_mirrorBlockPreview.transform.localPosition = (Vector2) mirrorRootPosition;
					_mirrorBlockPreview.transform.localRotation = TransformUtils.GetPhysicalRotation(mirrorRotation);
					_mirrorBlockPreview.SetActive(
						!GridMove.Dragging
						&& !ContainsMirrorPosition(
							blockSelection.BlockSpec, _hoverPosition.Value, _rotation, _mirrorPosition.Value
						)
					);
				}
				else
				{
					_mirrorBlockPreview.SetActive(false);
				}

				EraserIndicator.gameObject.SetActive(false);
				MirrorEraserIndicator.gameObject.SetActive(false);

				break;
			}
		}
	}

	private GameObject CreatePreview(BlockSpec blockSpec)
	{
		var preview = BlockBuilder.BuildFromSpec(
			blockSpec, PreviewParent, Vector2Int.zero, 0
		);

		foreach (SpriteRenderer sprite in preview.GetComponentsInChildren<SpriteRenderer>())
		{
			Color c = sprite.color;
			c.a *= 0.5f;
			sprite.color = c;
		}

		return preview;
	}

	private void UpdateEraserIndicator(Vector2Int hoverPosition, GameObject indicator)
	{
		VehicleBlueprint.BlockInstance blockInstance = Builder.GetBlockInstanceAt(hoverPosition);
		if (blockInstance == null)
		{
			indicator.SetActive(false);
			return;
		}

		BlockSpec blockSpec = BlockDatabase.Instance.GetBlockSpec(blockInstance.BlockId);
		BlockBounds blockBounds = new BlockBounds(
			blockSpec.Construction.BoundsMin, blockSpec.Construction.BoundsMax
		).Transformed(blockInstance.Position, blockInstance.Rotation);

		Vector2 center = blockBounds.Center - new Vector2(0.5f, 0.5f);
		indicator.transform.localPosition = center;
		indicator.GetComponent<SpriteRenderer>().size = blockBounds.Size;

		indicator.SetActive(true);
	}

	private void DestroyBlockPreview()
	{
		if (_blockPreview != null)
		{
			Destroy(_blockPreview);
			_blockPreview = null;
		}

		if (_mirrorBlockPreview != null)
		{
			Destroy(_mirrorBlockPreview);
			_mirrorBlockPreview = null;
		}
	}

	private void UpdatePaletteUse()
	{
		if (_usePosition == null) return;
		Vector2Int usePosition = _usePosition.Value;

		switch (Palette.CurrentSelection)
		{
			case BlockSelection blockSelection:
				TryAddBlock(blockSelection.BlockSpec, usePosition, _rotation, _prevUse == null);

				if (
					_mirrorPosition != null
					&& !ContainsMirrorPosition(blockSelection.BlockSpec, usePosition, _rotation, _mirrorPosition.Value)
				)
				{
					GetMirrorBlock(
						blockSelection.BlockSpec, usePosition, _rotation,
						_mirrorPosition.Value,
						out BlockSpec mirrorBlockSpec, out Vector2Int mirrorRootPosition, out int mirrorRotation
					);
					TryAddBlock(mirrorBlockSpec, mirrorRootPosition, mirrorRotation, _prevUse == null);
				}

				UpdateConflicts();
				UpdateDisconnections();

				break;
			case EraserSelection _:
				TryEraseBlock(usePosition, _prevUse == null);
				if (_mirrorPosition != null)
				{
					TryEraseBlock(new Vector2Int(_mirrorPosition.Value - usePosition.x, usePosition.y), false);
				}

				EraserIndicator.gameObject.SetActive(false);
				MirrorEraserIndicator.gameObject.SetActive(false);

				UpdateDisconnections();

				break;
		}
	}

	private static bool ContainsMirrorPosition(BlockSpec spec, Vector2Int position, int rotation, int mirror)
	{
		BlockBounds bounds = new BlockBounds(
			spec.Construction.BoundsMin, spec.Construction.BoundsMax
		).Transformed(position, rotation);
		return mirror >= bounds.Min.x * 2 && mirror <= (bounds.Max.x - 1) * 2;
	}

	private void TryAddBlock(BlockSpec blockSpec, Vector2Int rootPosition, int rotation, bool showFailText)
	{
		try
		{
			if (ReplacementModeAction.action.ReadValue<float>() > 0.5f)
			{
				var conflicts = Builder.GetConflicts(blockSpec, rootPosition, rotation);
				var conflictBlocks = new HashSet<VehicleBlueprint.BlockInstance>(
					conflicts.Select(position => Builder.GetBlockInstanceAt(position))
				);

				switch (conflictBlocks.Count)
				{
					case 0:
						break;
					case 1:
						try
						{
							Builder.RemoveBlock(conflictBlocks.First().Position);
						}
						catch (BlockNotErasable error)
						{
							if (showFailText)
							{
								string blockName = BlockDatabase.Instance.GetBlockSpec(error.BlockId).Info.FullName;
								FlytextManager.CreateNotificationFlytext(
									Builder.transform.TransformPoint(error.Position),
									$"{blockName} cannot be replaced"
								);
							}

							return;
						}

						break;
					default:
						if (showFailText)
						{
							FlytextManager.CreateNotificationFlytext(
								Builder.transform.TransformPoint((Vector2) rootPosition),
								"Overlaps multiple blocks"
							);
						}

						return;
				}
			}

			Builder.AddBlock(blockSpec, rootPosition, rotation);
		}
		catch (DuplicateBlockError error)
		{
			if (showFailText)
			{
				FlytextManager.CreateNotificationFlytext(
					Builder.transform.TransformPoint(error.Position),
					"Block overlap"
				);
			}
		}
	}

	private void TryEraseBlock(Vector2Int position, bool showFailText)
	{
		try
		{
			Builder.RemoveBlock(position);
		}
		catch (EmptyBlockError error)
		{
			if (showFailText)
			{
				FlytextManager.CreateNotificationFlytext(
					Builder.transform.TransformPoint(error.Position),
					"Nothing to erase"
				);
			}
		}
		catch (BlockNotErasable error)
		{
			if (showFailText)
			{
				string blockName = BlockDatabase.Instance.GetBlockSpec(error.BlockId).Info.FullName;
				FlytextManager.CreateNotificationFlytext(
					Builder.transform.TransformPoint(error.Position),
					$"{blockName} cannot be erased"
				);
			}
		}
	}

	private void UpdateConflicts()
	{
		if (Palette.CurrentSelection is BlockSelection blockSelection && _hoverPosition != null)
		{
			HashSet<Vector2Int> conflicts = new HashSet<Vector2Int>(
				Builder.GetConflicts(blockSelection.BlockSpec, _hoverPosition.Value, _rotation)
			);

			if (
				_mirrorPosition != null
				&& !ContainsMirrorPosition(
					blockSelection.BlockSpec, _hoverPosition.Value, _rotation, _mirrorPosition.Value
				)
			)
			{
				GetMirrorBlock(
					blockSelection.BlockSpec, _hoverPosition.Value, _rotation,
					_mirrorPosition.Value,
					out BlockSpec mirrorBlockSpec, out Vector2Int mirrorRootPosition, out int mirrorRotation
				);

				conflicts.UnionWith(Builder.GetConflicts(mirrorBlockSpec, mirrorRootPosition, mirrorRotation));
			}

			Indicators.SetConflicts(conflicts);
		}
		else
		{
			Indicators.SetConflicts(null);
		}
	}

	private void UpdateDisconnections()
	{
		Indicators.SetDisconnections(Builder.GetDisconnectedPositions());
	}

	#endregion

	private static void GetMirrorBlock(
		BlockSpec blockSpec, Vector2Int rootPosition, int rotation,
		int mirrorPosition,
		out BlockSpec mirrorBlockSpec, out Vector2Int mirrorRootPosition, out int mirrorRotation
	)
	{
		mirrorBlockSpec = BlockDatabase.Instance.GetBlockSpec(
			BlockDatabase.GetMirrorBlockId(blockSpec)
		);

		mirrorRotation =
			(4 - rotation + blockSpec.Construction.MirrorRotationOffset)
			% 4;
		mirrorRootPosition =
			new Vector2Int(mirrorPosition - rootPosition.x, rootPosition.y)
			+ TransformUtils.RotatePoint(
				mirrorBlockSpec.Construction.MirrorRootOffset,
				mirrorRotation
			);
	}

	private void HandleRotate(InputAction.CallbackContext context)
	{
		if (InverseRotateAction.action.ReadValue<float>() > 0.5f)
		{
			_rotation = (_rotation + 1) % 4;
		}
		else
		{
			_rotation = (_rotation + 3) % 4;
		}
	}

	private void OnPaletteSelectionChanged()
	{
		_selectionChanged = true;
	}
}
}