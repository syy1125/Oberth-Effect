using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.UserInterface;
using Syy1125.OberthEffect.Designer.Config;
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

	[Header("Block Notifications")]
	public GameObject BlockWarningPrefab;
	public Transform BlockWarningParent;

	public Color WarningColor = Color.yellow;
	public Color ErrorColor = Color.red;

	public float BlockTooltipDelay = 0.2f;

	[Header("Components")]
	public BlockPalette Palette;
	public VehicleBuilder Builder;
	public DesignerConfig Config;
	public DesignerCursorTexture CursorTexture;
	public VehicleAnalyzer Analyzer;

	[Header("Input Actions")]
	public InputActionReference RotateAction;

	public InputActionReference ClickAction;
	public InputActionReference ScrollAction;

	public InputActionReference PanAction;
	public InputActionReference DragAction;

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

	private GameObject _preview;
	private bool _prevHover;
	private int _prevIndex;
	private Vector2Int? _prevHoverLocation;
	private Vector2Int? _prevTooltipLocation;
	private bool _prevDragging;
	private Vector2Int? _prevClick;

	private float _zoomScale;
	private Vector2Int? _clickLocation;
	private Vector3? _dragHandle;
	private bool Dragging => _dragHandle != null;
	public Vector2Int HoverLocation { get; private set; }
	private Vector2Int? _tooltipLocation;

	private HashSet<Vector2Int> _conflicts;
	private HashSet<Vector2Int> _disconnections;
	private Dictionary<Vector2Int, GameObject> _warningObjects;
	private bool _warningChanged;

	#endregion

	private void Awake()
	{
		_mainCamera = Camera.main;

		_conflicts = new HashSet<Vector2Int>();
		_disconnections = new HashSet<Vector2Int>();
		_warningObjects = new Dictionary<Vector2Int, GameObject>();
		_warningChanged = false;
	}

	#region Enable and Disable

	private void OnEnable()
	{
		EnableActions();
		RotateAction.action.performed += HandleRotate;
		DebugAction.action.performed += HandleDebug;

		UpdateCursor();
	}

	private void EnableActions()
	{
		RotateAction.action.Enable();
		ClickAction.action.Enable();
		ScrollAction.action.Enable();
		PanAction.action.Enable();
		DragAction.action.Enable();
		DebugAction.action.Enable();
	}

	private void OnDisable()
	{
		RotateAction.action.performed -= HandleRotate;
		DebugAction.action.performed -= HandleDebug;
		DisableActions();

		CursorTexture.TargetStatus = DesignerCursorTexture.CursorStatus.Default;
	}

	private void DisableActions()
	{
		RotateAction.action.Disable();
		ClickAction.action.Disable();
		ScrollAction.action.Disable();
		PanAction.action.Disable();
		DragAction.action.Disable();
		DebugAction.action.Disable();
	}

	#endregion

	private void Start()
	{
		Vector3 areaCenter = AreaMask.GetComponent<RectTransform>().position;
		transform.position = new Vector3(areaCenter.x, areaCenter.y, transform.position.z);

		_zoomScale = 1;
		transform.localScale = Vector3.one * _zoomScale;

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

	#region Update

	private void Update()
	{
		UpdateHover();
		UpdateDragState();
		UpdateScroll();
		UpdateClick();

		if (
			Palette.SelectedIndex != _prevIndex
			|| HoverLocation != _prevHoverLocation
			|| AreaMask.Hover != _prevHover
			|| Dragging != _prevDragging
		)
		{
			UpdatePreview();
			UpdateConflicts();
			UpdateTooltip();
		}

		if (Dragging != _prevDragging || Palette.SelectedIndex != _prevIndex)
		{
			UpdateCursor();
		}

		if (_clickLocation != _prevClick)
		{
			UpdatePaletteUse();
		}

		if (_warningChanged)
		{
			UpdateBlockWarnings();
			_warningChanged = false;
		}

		UpdateDragPosition();

		_prevDragging = Dragging;
		_prevHover = AreaMask.Hover;
		_prevIndex = Palette.SelectedIndex;
		_prevHoverLocation = HoverLocation;
		_prevTooltipLocation = _tooltipLocation;
		_prevClick = _clickLocation;
	}

	private void UpdateHover()
	{
		Vector2 mousePosition = Mouse.current.position.ReadValue();
		Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(mousePosition);
		Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
		HoverLocation = new Vector2Int(Mathf.RoundToInt(localPosition.x), Mathf.RoundToInt(localPosition.y));
	}

	private void UpdateDragState()
	{
		bool dragging = AreaMask.Hover && DragAction.action.ReadValue<float>() > 0.5f;

		if (dragging && !_prevDragging)
		{
			_dragHandle = GetLocalMousePosition();
		}
		else if (!dragging)
		{
			_dragHandle = null;
		}
	}

	private void UpdateScroll()
	{
		var scroll = ScrollAction.action.ReadValue<float>();
		Vector3 mouseWorldPosition = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

		if (AreaMask.Hover && Mathf.Abs(scroll) > Mathf.Epsilon)
		{
			Vector3 oldLocalPosition = transform.InverseTransformPoint(mouseWorldPosition);

			_zoomScale = Mathf.Clamp(_zoomScale * Mathf.Exp(scroll / 10f), 0.1f, 10f);
			transform.localScale = Vector3.one * _zoomScale;

			Vector3 newLocalPosition = transform.InverseTransformPoint(mouseWorldPosition);
			Vector3 worldDelta = transform.TransformVector(newLocalPosition - oldLocalPosition);

			transform.position += worldDelta;
		}
	}

	private void UpdateClick()
	{
		bool click = ClickAction.action.ReadValue<float>() > 0.5f && !Dragging && AreaMask.Hover;

		if (click)
		{
			_clickLocation = HoverLocation;
		}
		else
		{
			_clickLocation = null;
		}
	}

	private void UpdatePreview()
	{
		if (Palette.SelectedIndex < 0)
		{
			if (_preview != null)
			{
				Destroy(_preview);
			}
		}
		else
		{
			if (Palette.SelectedIndex != _prevIndex)
			{
				if (_preview != null)
				{
					Destroy(_preview);
				}

				_preview = Instantiate(Palette.GetSelectedBlock(), transform);
				_preview.transform.rotation = transform.rotation * TransformUtils.GetPhysicalRotation(_rotation);
				_preview.SetActive(AreaMask.Hover);

				foreach (SpriteRenderer sprite in _preview.GetComponentsInChildren<SpriteRenderer>())
				{
					Color c = sprite.color;
					c.a *= 0.5f;
					sprite.color = c;
				}
			}

			_preview.transform.localPosition = new Vector3(HoverLocation.x, HoverLocation.y);

			if (AreaMask.Hover != _prevHover || Dragging != _prevDragging)
			{
				_preview.SetActive(AreaMask.Hover && !Dragging);
			}
		}
	}

	private void UpdateCursor()
	{
		if (Dragging)
		{
			CursorTexture.TargetStatus = DesignerCursorTexture.CursorStatus.Drag;
		}
		else if (Palette.SelectedIndex == BlockPalette.ERASE_INDEX)
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
		if (_clickLocation == null) return;

		if (Palette.SelectedIndex >= 0)
		{
			GameObject block = Palette.GetSelectedBlock();

			try
			{
				Builder.AddBlock(block, HoverLocation, _rotation);
				UpdateDisconnections();
			}
			catch (DuplicateBlockError error)
			{
				// TODO
			}
		}
		else
		{
			switch (Palette.SelectedIndex)
			{
				case BlockPalette.ERASE_INDEX:
					try
					{
						Builder.RemoveBlock(HoverLocation);
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
	}

	private void UpdateConflicts()
	{
		GameObject block = Palette.GetSelectedBlock();
		_conflicts = block == null
			? new HashSet<Vector2Int>()
			: new HashSet<Vector2Int>(Builder.GetConflicts(block, HoverLocation, _rotation));
		_warningChanged = true;
	}

	private void UpdateTooltip()
	{
		if (AreaMask.Hover && Palette.SelectedIndex == BlockPalette.CURSOR_INDEX && !Dragging)
		{
			_tooltipLocation = HoverLocation;
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
		_disconnections = new HashSet<Vector2Int>(Builder.GetDisconnectedPositions());
		_warningChanged = true;
	}

	private void UpdateBlockWarnings()
	{
		var newDisconnections = new HashSet<Vector2Int>(_disconnections);
		var newConflicts = new HashSet<Vector2Int>(_conflicts);

		foreach (KeyValuePair<Vector2Int, GameObject> pair in _warningObjects.ToArray())
		{
			bool isConflict = newConflicts.Remove(pair.Key);
			bool isDisconnected = newDisconnections.Remove(pair.Key);

			if (isConflict)
			{
				pair.Value.GetComponent<SpriteRenderer>().color = ErrorColor;
			}
			else if (isDisconnected)
			{
				pair.Value.GetComponent<SpriteRenderer>().color = WarningColor;
			}
			else
			{
				Destroy(pair.Value);
				_warningObjects.Remove(pair.Key);
			}
		}

		foreach (Vector2Int conflict in newConflicts)
		{
			GameObject go = Instantiate(BlockWarningPrefab, BlockWarningParent);
			go.transform.localPosition = new Vector3(conflict.x, conflict.y);
			go.GetComponent<SpriteRenderer>().color = ErrorColor;

			_warningObjects.Add(conflict, go);

			newDisconnections.Remove(conflict);
		}

		foreach (Vector2Int disconnection in newDisconnections)
		{
			GameObject go = Instantiate(BlockWarningPrefab, BlockWarningParent);
			go.transform.localPosition = new Vector3(disconnection.x, disconnection.y);
			go.GetComponent<SpriteRenderer>().color = WarningColor;

			_warningObjects.Add(disconnection, go);
		}
	}

	private void UpdateDragPosition()
	{
		if (_dragHandle != null)
		{
			Vector3 mousePosition = GetLocalMousePosition();
			transform.position += transform.TransformVector(mousePosition - _dragHandle.Value);
		}
		else
		{
			transform.Translate(Time.deltaTime * -4f * (Vector3) PanAction.action.ReadValue<Vector2>(), Space.Self);
		}
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

		if (_preview != null)
		{
			_preview.transform.rotation = Quaternion.AngleAxis(_rotation * 90f, Vector3.forward);
		}
	}

	#endregion

	public List<string> GetVehicleErrors()
	{
		var messages = new List<string>();

		if (_disconnections.Count > 0)
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
		Debug.Log($"Palette selection index {Palette.SelectedIndex}");
		VehicleBlueprint.BlockInstance hoverBlock = Builder.GetBlockInstanceAt(HoverLocation);
		Debug.Log(
			hoverBlock == null
				? $"Hovering at {HoverLocation} over null"
				: $"Hovering at {HoverLocation} over {hoverBlock.BlockID} at ({hoverBlock.X}, {hoverBlock.Y}) with rotation {hoverBlock.Rotation}"
		);
	}

	#endregion
}
}