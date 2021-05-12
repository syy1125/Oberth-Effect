using System.Collections.Generic;
using System.Linq;
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

	[Header("Components")]
	public BlockPalette Palette;

	public VehicleBuilder Builder;

	public DesignerCursor Cursor;

	[Header("Input Actions")]
	public InputActionReference RotateAction;

	public InputActionReference ClickAction;
	public InputActionReference ScrollAction;

	public InputActionReference PanAction;
	public InputActionReference DragAction;
	public InputActionReference MouseMoveAction;

	#endregion

	#region Private Fields

	private Camera _mainCamera;

	private int _rotation;

	private GameObject _preview;
	private bool _prevHover;
	private int _prevIndex;
	private Vector2Int? _prevLocation;
	private bool _prevDragging;
	private Vector2Int? _prevClick;

	private Vector2Int? _clickLocation;
	private Vector3? _dragHandle;
	private bool Dragging => _dragHandle != null;
	private Vector2Int _hoverLocation;
	private Vector2Int? _selectedLocation;

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
		Palette.OnIndexChanged += HandleIndexChange;

		EnableActions();
		RotateAction.action.performed += HandleRotate;

		UpdateCursor();
	}

	private void EnableActions()
	{
		RotateAction.action.Enable();
		ClickAction.action.Enable();
		ScrollAction.action.Enable();
		PanAction.action.Enable();
		DragAction.action.Enable();
		MouseMoveAction.action.Enable();
	}

	private void OnDisable()
	{
		Palette.OnIndexChanged -= HandleIndexChange;

		RotateAction.action.performed -= HandleRotate;
		DisableActions();

		Cursor.TargetStatus = DesignerCursor.CursorStatus.Default;
	}

	private void DisableActions()
	{
		RotateAction.action.Disable();
		ClickAction.action.Disable();
		ScrollAction.action.Disable();
		PanAction.action.Disable();
		DragAction.action.Disable();
		MouseMoveAction.action.Disable();
	}

	#endregion

	private void Start()
	{
		Vector3 areaCenter = AreaMask.GetComponent<RectTransform>().position;
		transform.position = new Vector3(areaCenter.x, areaCenter.y, transform.position.z);
	}

	#region Update

	private void Update()
	{
		UpdateHover();
		UpdateDragging();
		UpdateScroll();
		UpdateClick();

		if (
			Palette.SelectedIndex != _prevIndex
			|| _hoverLocation != _prevLocation
			|| AreaMask.Hover != _prevHover
			|| Dragging != _prevDragging
		)
		{
			UpdatePreview();
			UpdateConflicts();
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
	}

	private void UpdateHover()
	{
		Vector2 mousePosition = Mouse.current.position.ReadValue();
		Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(mousePosition);
		Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
		_hoverLocation = new Vector2Int(Mathf.RoundToInt(localPosition.x), Mathf.RoundToInt(localPosition.y));
	}

	private void UpdateDragging()
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

			transform.localScale *= Mathf.Exp(scroll / 10f);

			Vector3 newLocalPosition = transform.InverseTransformPoint(mouseWorldPosition);
			Vector3 worldDelta = transform.TransformVector(newLocalPosition - oldLocalPosition);

			transform.position += worldDelta;
		}
	}

	private void UpdateClick()
	{
		bool click = ClickAction.action.ReadValue<float>() > 0.5f && !Dragging;

		if (click)
		{
			_clickLocation = _hoverLocation;
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
				_preview.transform.rotation = transform.rotation * RotationUtils.GetPhysicalRotation(_rotation);
				_preview.SetActive(AreaMask.Hover);

				foreach (SpriteRenderer sprite in _preview.GetComponentsInChildren<SpriteRenderer>())
				{
					Color c = sprite.color;
					c.a *= 0.5f;
					sprite.color = c;
				}
			}

			_preview.transform.localPosition = new Vector3(_hoverLocation.x, _hoverLocation.y);

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
			Cursor.TargetStatus = DesignerCursor.CursorStatus.Drag;
		}
		else if (Palette.SelectedIndex == BlockPalette.ERASE_INDEX)
		{
			Cursor.TargetStatus = DesignerCursor.CursorStatus.Eraser;
		}
		else
		{
			Cursor.TargetStatus = DesignerCursor.CursorStatus.Default;
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
				Builder.AddBlock(block, _hoverLocation, _rotation);
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
				case BlockPalette.CURSOR_INDEX:
					_selectedLocation = _hoverLocation;
					break;
				case BlockPalette.ERASE_INDEX:
					try
					{
						Builder.RemoveBlock(_hoverLocation);
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
			: new HashSet<Vector2Int>(Builder.GetConflicts(block, _hoverLocation, _rotation));
		_warningChanged = true;
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

	private void LateUpdate()
	{
		_prevDragging = Dragging;
		_prevHover = AreaMask.Hover;
		_prevIndex = Palette.SelectedIndex;
		_prevLocation = _hoverLocation;
		_prevClick = _clickLocation;

		if (_dragHandle != null)
		{
			Vector3 mousePosition = GetLocalMousePosition();
			transform.position += transform.TransformVector(mousePosition - _dragHandle.Value);
		}
		else
		{
			transform.position -= Time.deltaTime * 2.5f * (Vector3) PanAction.action.ReadValue<Vector2>();
		}
	}

	#endregion

	private Vector3 GetLocalMousePosition()
	{
		return transform.InverseTransformPoint(_mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue()));
	}

	private void HandleIndexChange()
	{
		if (Palette.SelectedIndex != BlockPalette.CURSOR_INDEX)
		{
			_selectedLocation = null;
		}
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

	// private void HandleClick(InputAction.CallbackContext context)
	// {
	// 	if (AreaMask.Hover)
	// 	{
	// 		if (Palette.SelectedIndex >= 0)
	// 		{
	// 			GameObject block = Palette.GetSelectedBlock();
	//
	// 			try
	// 			{
	// 				Builder.AddBlock(block, _hoverLocation, _rotation);
	// 				UpdateDisconnections();
	// 			}
	// 			catch (DuplicateBlockError error)
	// 			{
	// 				// TODO
	// 			}
	// 		}
	// 		else
	// 		{
	// 			switch (Palette.SelectedIndex)
	// 			{
	// 				case BlockPalette.CURSOR_INDEX:
	// 					_selectedLocation = _hoverLocation;
	// 					break;
	// 				case BlockPalette.ERASE_INDEX:
	// 					try
	// 					{
	// 						Builder.RemoveBlock(_hoverLocation);
	// 						UpdateDisconnections();
	// 					}
	// 					catch (EmptyBlockError)
	// 					{
	// 						// TODO
	// 					}
	// 					catch (BlockNotErasable)
	// 					{
	// 						// TODO
	// 					}
	//
	// 					break;
	// 			}
	// 		}
	// 	}
	// }

	#endregion

	public string SaveVehicle()
	{
		return Builder.SaveVehicle();
	}

	public void LoadVehicle(string blueprint)
	{
		Builder.LoadVehicle(blueprint);
	}
}
}