using System.Collections.Generic;
using Syy1125.OberthEffect.Common;
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
	public InputActionAsset InputActions;
	public string DesignerActionMapName;
	public InputActionReference DebugAction;

	#endregion

	private Camera _mainCamera;
	public VehicleBlueprint Blueprint { get; private set; }

	public Vector2 HoverPosition { get; private set; }
	public Vector2Int HoverPositionInt { get; private set; }

	private void Awake()
	{
		_mainCamera = Camera.main;
	}

	#region Enable and Disable

	private void OnEnable()
	{
		InputActions.FindActionMap(DesignerActionMapName, true).Enable();

		DebugAction.action.performed += HandleDebug;

		UpdateCursor();
	}

	private void OnDisable()
	{
		DebugAction.action.performed -= HandleDebug;

		InputActions.FindActionMap(DesignerActionMapName, true).Disable();

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