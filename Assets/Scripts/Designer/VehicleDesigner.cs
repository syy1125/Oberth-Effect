using System.Collections.Generic;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.UserInterface;
using Syy1125.OberthEffect.Designer.Config;
using Syy1125.OberthEffect.Designer.Palette;
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
	public DesignerGridMove GridMove;
	public DesignerPaletteUse PaletteUse;
	public VehicleBuilder Builder;
	public DesignerConfig Config;
	public DesignerCursorTexture CursorTexture;
	public VehicleAnalyzer Analyzer;

	[Header("Input Actions")]
	public InputActionReference DebugAction;

	#endregion

	#region Public Properties

	public VehicleBlueprint Blueprint { get; private set; }

	#endregion

	#region Private Components

	private Camera _mainCamera;

	#endregion

	#region Private States

	// Change detection
	private bool _prevHovering;
	private Vector2Int? _prevHoverPosition;
	private Vector2Int? _prevTooltipLocation;
	private bool _prevDragging;

	public Vector2 HoverPosition { get; private set; }
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
		DebugAction.action.Enable();
		DebugAction.action.performed += HandleDebug;

		UpdateCursor();
	}

	private void OnDisable()
	{
		DebugAction.action.performed -= HandleDebug;
		DebugAction.action.Disable();

		CursorTexture.TargetStatus = DesignerCursorTexture.CursorStatus.Default;
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
			PaletteUse.InitVehicle();
			Config.ReloadVehicle();
		}
	}

	#region Update

	private void Update()
	{
		UpdateMousePosition();
		UpdateCursor();

		if (
			HoverPositionInt != _prevHoverPosition
			|| AreaMask.Hovering != _prevHovering
			|| GridMove.Dragging != _prevDragging
		)
		{
			UpdateTooltip();
		}

		_prevDragging = GridMove.Dragging;
		_prevHovering = AreaMask.Hovering;
		_prevHoverPosition = HoverPositionInt;
		_prevTooltipLocation = _tooltipLocation;
	}

	private void UpdateMousePosition()
	{
		Vector2 mousePosition = Mouse.current.position.ReadValue();
		Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(mousePosition);
		HoverPosition = transform.InverseTransformPoint(worldPosition);
		HoverPositionInt = Vector2Int.RoundToInt(HoverPosition);
	}

	private void UpdateCursor()
	{
		if (GridMove.Dragging)
		{
			CursorTexture.TargetStatus = DesignerCursorTexture.CursorStatus.Drag;
		}
		else if (PaletteUse.CurrentSelection is EraserSelection)
		{
			CursorTexture.TargetStatus = DesignerCursorTexture.CursorStatus.Eraser;
		}
		else
		{
			CursorTexture.TargetStatus = DesignerCursorTexture.CursorStatus.Default;
		}
	}

	private void UpdateTooltip()
	{
		if (AreaMask.Hovering && PaletteUse.CurrentSelection is CursorSelection && !GridMove.Dragging)
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

	#endregion

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
		PaletteUse.ReloadVehicle();
		Config.ReloadVehicle();
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