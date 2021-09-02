using System.Collections.Generic;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.UserInterface;
using Syy1125.OberthEffect.Designer.Config;
using Syy1125.OberthEffect.Designer.Palette;
using Syy1125.OberthEffect.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Designer
{
// A high-level controller for the designer
public class VehicleDesigner : MonoBehaviour
{
	#region Public Fields

	[Header("References")]
	public DesignerAreaMask AreaMask;

	[Header("Config")]
	public float BlockTooltipDelay = 0.2f;

	[Header("Components")]
	public DesignerVehicleMove VehicleMove;
	public BlockPalette Palette;
	public VehicleBuilder Builder;
	public BlockIndicators Indicators;
	public DesignerConfig Config;
	public DesignerCursorTexture CursorTexture;
	public VehicleAnalyzer Analyzer;

	[Header("Input Actions")]
	public InputActionReference RotateAction;
	public InputActionReference ClickAction;
	public InputActionReference DebugAction;

	#endregion

	#region Public Properties

	public VehicleBlueprint Blueprint { get; private set; }

	#endregion

	#region Private Components

	private Camera _mainCamera;

	#endregion

	#region Private States

	private int _rotation;

	private GameObject _paletteActionPreview;

	// Change detection
	private bool _prevHovering;
	private bool _paletteSelectionChanged;
	private Vector2Int? _prevHoverPosition;
	private Vector2Int? _prevTooltipLocation;
	private bool _prevDragging;
	private Vector2Int? _prevClick;

	private Vector2Int? _clickPosition;
	public Vector2Int HoverPositionInt { get; private set; }
	private Vector2Int? _tooltipLocation;

	#endregion

	private void Awake()
	{
		_mainCamera = Camera.main;
	}

	#region Enable and Disable

	private void OnEnable()
	{
		EnableActions();
		RotateAction.action.performed += HandleRotate;
		DebugAction.action.performed += HandleDebug;

		Palette.OnSelectionChanged += OnPaletteSelectionChanged;

		UpdateCursor();
	}

	private void EnableActions()
	{
		RotateAction.action.Enable();
		ClickAction.action.Enable();
		DebugAction.action.Enable();
	}

	private void OnDisable()
	{
		RotateAction.action.performed -= HandleRotate;
		DebugAction.action.performed -= HandleDebug;
		DisableActions();

		Palette.OnSelectionChanged -= OnPaletteSelectionChanged;

		CursorTexture.TargetStatus = DesignerCursorTexture.CursorStatus.Default;
	}

	private void DisableActions()
	{
		RotateAction.action.Disable();
		ClickAction.action.Disable();
		DebugAction.action.Disable();
	}

	#endregion

	private void Start()
	{
		Vector3 areaCenter = AreaMask.GetComponent<RectTransform>().position;
		transform.position = new Vector3(areaCenter.x, areaCenter.y, transform.position.z);

		if (VehicleSelection.SerializedVehicle != null)
		{
			ImportVehicle(VehicleSelection.SerializedVehicle);
			VehicleSelection.SerializedVehicle = null;
		}
		else
		{
			Blueprint = new VehicleBlueprint();
			Builder.InitVehicle();
			Config.ReloadVehicle();
		}
	}

	#region Event Handlers

	private void OnPaletteSelectionChanged()
	{
		_paletteSelectionChanged = true;
	}

	#endregion

	#region Update

	private void Update()
	{
		UpdateMousePosition();
		UpdateClick();

		if (
			_paletteSelectionChanged
			|| HoverPositionInt != _prevHoverPosition
			|| AreaMask.Hovering != _prevHovering
			|| VehicleMove.Dragging != _prevDragging
		)
		{
			UpdatePreview();
			UpdateConflicts();
			UpdateTooltip();
		}

		if (VehicleMove.Dragging != _prevDragging || _paletteSelectionChanged)
		{
			UpdateCursor();
		}

		if (_clickPosition != _prevClick)
		{
			UpdatePaletteUse();
		}

		_prevDragging = VehicleMove.Dragging;
		_prevHovering = AreaMask.Hovering;
		_paletteSelectionChanged = false;
		_prevHoverPosition = HoverPositionInt;
		_prevTooltipLocation = _tooltipLocation;
		_prevClick = _clickPosition;
	}

	private void UpdateMousePosition()
	{
		Vector2 mousePosition = Mouse.current.position.ReadValue();
		Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(mousePosition);
		Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
		HoverPositionInt = Vector2Int.RoundToInt(localPosition);
	}

	private void UpdateClick()
	{
		bool click = ClickAction.action.ReadValue<float>() > 0.5f && !VehicleMove.Dragging && AreaMask.Hovering;

		if (click)
		{
			_clickPosition = HoverPositionInt;
		}
		else
		{
			_clickPosition = null;
		}
	}

	private void UpdatePreview()
	{
		switch (Palette.CurrentSelection)
		{
			case CursorSelection _:
			case EraserSelection _:
				if (_paletteActionPreview != null)
				{
					Destroy(_paletteActionPreview);
					_paletteActionPreview = null;
				}

				break;

			case BlockSelection blockSelection:
				if (_paletteSelectionChanged)
				{
					if (_paletteActionPreview != null)
					{
						Destroy(_paletteActionPreview);
					}

					_paletteActionPreview = BlockBuilder.BuildFromSpec(
						blockSelection.BlockSpec, transform, HoverPositionInt, _rotation
					);
					_paletteActionPreview.SetActive(AreaMask.Hovering && !VehicleMove.Dragging);

					foreach (SpriteRenderer sprite in _paletteActionPreview.GetComponentsInChildren<SpriteRenderer>())
					{
						Color c = sprite.color;
						c.a *= 0.5f;
						sprite.color = c;
					}
				}
				else
				{
					_paletteActionPreview.transform.localPosition = new Vector3(HoverPositionInt.x, HoverPositionInt.y);
					_paletteActionPreview.transform.localRotation = TransformUtils.GetPhysicalRotation(_rotation);

					if (AreaMask.Hovering != _prevHovering || VehicleMove.Dragging != _prevDragging)
					{
						_paletteActionPreview.SetActive(AreaMask.Hovering && !VehicleMove.Dragging);
					}
				}

				break;
		}
	}

	private void UpdateCursor()
	{
		if (VehicleMove.Dragging)
		{
			CursorTexture.TargetStatus = DesignerCursorTexture.CursorStatus.Drag;
		}
		else if (Palette.CurrentSelection is EraserSelection)
		{
			CursorTexture.TargetStatus = DesignerCursorTexture.CursorStatus.Eraser;
		}
		else
		{
			CursorTexture.TargetStatus = DesignerCursorTexture.CursorStatus.Default;
		}
	}

	private void UpdatePaletteUse()
	{
		if (_clickPosition == null) return;
		Vector2Int position = _clickPosition.Value;

		if (Palette.isActiveAndEnabled)
		{
			switch (Palette.CurrentSelection)
			{
				case BlockSelection blockSelection:
					try
					{
						Builder.AddBlock(blockSelection.BlockSpec, position, _rotation);
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
						Builder.RemoveBlock(position);
						UpdateDisconnections();
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

		if (Analyzer.isActiveAndEnabled)
		{
			Analyzer.SetTargetBlockPosition(position);
		}
	}

	private void UpdateConflicts()
	{
		if (Palette.CurrentSelection is BlockSelection blockSelection)
		{
			Indicators.SetConflicts(
				Builder.GetConflicts(blockSelection.BlockSpec, HoverPositionInt, _rotation)
			);
		}
		else
		{
			Indicators.SetConflicts(null);
		}
	}

	private void UpdateTooltip()
	{
		if (AreaMask.Hovering && Palette.CurrentSelection is CursorSelection && !VehicleMove.Dragging)
		{
			_tooltipLocation = HoverPositionInt;
		}
		else
		{
			_tooltipLocation = null;
		}

		if (_tooltipLocation != _prevTooltipLocation)
		{
			if (_prevTooltipLocation != null)
			{
				CancelInvoke(nameof(ShowBlockTooltip));
				HideTooltip();
			}

			if (_tooltipLocation != null)
			{
				Invoke(nameof(ShowBlockTooltip), BlockTooltipDelay);
			}
		}
	}

	private void UpdateDisconnections()
	{
		Indicators.SetDisconnections(Builder.GetDisconnectedPositions());
	}

	#endregion

	private Vector3 GetLocalMousePosition()
	{
		return transform.InverseTransformPoint(_mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue()));
	}

	private void ShowBlockTooltip()
	{
		if (!isActiveAndEnabled) return;
		if (_tooltipLocation == null || TooltipControl.Instance == null) return;
		GameObject go = Builder.GetBlockObjectAt(_tooltipLocation.Value);
		if (go == null) return;

		TooltipControl.Instance.SetTooltip(TooltipProviderUtils.CombineTooltips(go));
	}

	private void HideTooltip()
	{
		if (TooltipControl.Instance == null) return;

		TooltipControl.Instance.SetTooltip(null);
	}

	#region Block Placement

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

	#endregion

	public List<string> GetVehicleErrors()
	{
		var messages = new List<string>();

		if (Builder.GetDisconnectedPositions().Count > 0)
		{
			messages.Add("Some blocks are disconnected");
		}

		return messages;
	}

	public string ExportVehicle()
	{
		return JsonUtility.ToJson(Blueprint);
	}

	public void ImportVehicle(string blueprint)
	{
		Blueprint = JsonUtility.FromJson<VehicleBlueprint>(blueprint);
		Config.ReloadVehicle();
		Builder.ReloadVehicle();
		Analyzer.StartAnalysis();
	}

	#region Debug

	private void HandleDebug(InputAction.CallbackContext context)
	{
		// Debug.Log($"Palette selection index {Palette.SelectedIndex}");
		VehicleBlueprint.BlockInstance hoverBlock = Builder.GetBlockInstanceAt(HoverPositionInt);
		Debug.Log(
			hoverBlock == null
				? $"Hovering at {HoverPositionInt} over null"
				: $"Hovering at {HoverPositionInt} over {hoverBlock.BlockId} at ({hoverBlock.Position}) with rotation {hoverBlock.Rotation}"
		);
	}

	#endregion
}
}