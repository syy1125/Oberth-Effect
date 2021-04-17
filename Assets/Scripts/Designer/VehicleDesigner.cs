using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

class DuplicateBlockError : Exception
{ }

class EmptyBlockError : Exception
{ }

class BlockNotErasable : Exception
{ }

[RequireComponent(typeof(Grid))]
public class VehicleDesigner : MonoBehaviour
{
	#region Public Fields

	[Header("References")]
	public BlockPalette Palette;

	public DesignerAreaMask AreaMask;

	public GameObject ControlCoreBlock;

	[Header("Input Actions")]
	public InputActionReference RotateAction;

	public InputActionReference ClickAction;
	public InputActionReference ScrollAction;

	public InputActionReference PanAction;
	public InputActionReference DragAction;
	public InputActionReference MouseMoveAction;

	[Header("Grabbing")]
	public Texture2D GrabTexture;

	public Texture2D EraserTexture;

	#endregion

	#region Private Fields

	private Camera _mainCamera;
	private Grid _grid;

	private int _rotation;

	private GameObject _preview;
	private bool _prevHover;
	private int _prevIndex;
	private Vector2Int? _prevLocation;
	private bool _prevDragging;

	private bool _dragging;
	private Vector2Int _hoverLocation;
	private Vector2Int? _selectedLocation;
	private VehicleBlueprint _blueprint;
	private Dictionary<Vector2Int, VehicleBlueprint.BlockInstance> _posToBlock;
	private Dictionary<VehicleBlueprint.BlockInstance, Vector2Int[]> _blockToPos;
	private Dictionary<VehicleBlueprint.BlockInstance, GameObject> _blockToObject;

	#endregion

	private void Awake()
	{
		_mainCamera = Camera.main;
		_grid = GetComponent<Grid>();

		_blueprint = new VehicleBlueprint();
		_posToBlock = new Dictionary<Vector2Int, VehicleBlueprint.BlockInstance>();
		_blockToPos = new Dictionary<VehicleBlueprint.BlockInstance, Vector2Int[]>();
		_blockToObject = new Dictionary<VehicleBlueprint.BlockInstance, GameObject>();
	}

	#region Enable and Disable

	private void OnEnable()
	{
		Palette.OnIndexChanged += HandleIndexChange;

		EnableActions();
		RotateAction.action.performed += HandleRotate;
		ClickAction.action.performed += HandleClick;
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
		ClickAction.action.performed -= HandleClick;
		DisableActions();

		Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
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

		InitVehicle();
	}

	private Vector2Int GetHoverLocation()
	{
		Vector2 mousePosition = Mouse.current.position.ReadValue();
		Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(mousePosition);
		Vector3Int gridPosition = _grid.WorldToCell(worldPosition);
		return new Vector2Int(gridPosition.x, gridPosition.y);
	}

	#region Update

	private void Update()
	{
		UpdateDragging();
		UpdateScroll();
		UpdateHover();
		UpdateCursor();
	}

	private void UpdateDragging()
	{
		_dragging = AreaMask.Hover && DragAction.action.ReadValue<float>() > 0.5f;
	}

	private void UpdateScroll()
	{
		var scroll = ScrollAction.action.ReadValue<Vector2>();
		Vector3 mouseWorldPosition = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

		if (AreaMask.Hover && Mathf.Abs(scroll.y) > Mathf.Epsilon)
		{
			Vector3 oldLocalPosition = transform.InverseTransformPoint(mouseWorldPosition);

			float zoom = Mathf.Sign(scroll.y);
			transform.localScale *= Mathf.Exp(zoom / 10f);

			Vector3 newLocalPosition = transform.InverseTransformPoint(mouseWorldPosition);
			Vector3 worldDelta = transform.TransformVector(newLocalPosition - oldLocalPosition);

			transform.position += worldDelta;
		}
	}

	private void UpdateHover()
	{
		_hoverLocation = GetHoverLocation();
		if (
			Palette.SelectedIndex != _prevIndex
			|| _hoverLocation != _prevLocation
			|| AreaMask.Hover != _prevHover
			|| _dragging != _prevDragging
		)
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

				_preview.transform.position =
					_grid.GetCellCenterWorld(new Vector3Int(_hoverLocation.x, _hoverLocation.y, 0));

				if (AreaMask.Hover != _prevHover || _dragging != _prevDragging)
				{
					_preview.SetActive(AreaMask.Hover && !_dragging);
				}
			}
		}
	}

	private void UpdateCursor()
	{
		if (_dragging != _prevDragging || Palette.SelectedIndex != _prevIndex)
		{
			if (_dragging)
			{
				Cursor.SetCursor(GrabTexture, new Vector2(50f, 50f), CursorMode.Auto);
			}
			else if (Palette.SelectedIndex == BlockPalette.ERASE_INDEX)
			{
				Cursor.SetCursor(EraserTexture, new Vector2(10, 10), CursorMode.Auto);
			}
			else
			{
				Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
			}
		}
	}

	private void LateUpdate()
	{
		_prevDragging = _dragging;
		_prevHover = AreaMask.Hover;
		_prevIndex = Palette.SelectedIndex;
		_prevLocation = _hoverLocation;

		if (_dragging)
		{
			var mouseMove = MouseMoveAction.action.ReadValue<Vector2>();
			Vector3 worldDelta = _mainCamera.ScreenToWorldPoint(mouseMove)
			                     - _mainCamera.ScreenToWorldPoint(Vector3.zero);
			transform.position += worldDelta;
		}
		else
		{
			transform.position -= Time.deltaTime * 2.5f * (Vector3) PanAction.action.ReadValue<Vector2>();
		}
	}

	#endregion

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

	private void HandleClick(InputAction.CallbackContext context)
	{
		Vector2Int hoverLocation = GetHoverLocation();
		if (AreaMask.Hover)
		{
			var rootLocation = new Vector2Int(hoverLocation.x, hoverLocation.y);

			if (Palette.SelectedIndex >= 0)
			{
				GameObject block = Palette.GetSelectedBlock();

				try
				{
					AddBlock(block, rootLocation);
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
						_selectedLocation = rootLocation;
						break;
					case BlockPalette.ERASE_INDEX:
						try
						{
							RemoveBlock(rootLocation);
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
	}

	private void AddBlock(GameObject block, Vector2Int rootLocation)
	{
		var info = block.GetComponent<BlockInfo>();

		var positions = new List<Vector2Int>();

		foreach (Vector3Int localPosition in info.Bounds.allPositionsWithin)
		{
			Vector2Int globalPosition = rootLocation + RotationUtils.RotatePoint(localPosition, _rotation);

			if (_posToBlock.ContainsKey(globalPosition))
			{
				throw new DuplicateBlockError();
			}

			positions.Add(globalPosition);
		}

		var instance = new VehicleBlueprint.BlockInstance
		{
			BlockID = info.BlockID,
			X = rootLocation.x,
			Y = rootLocation.y,
			Rotation = _rotation
		};

		_blueprint.Blocks.Add(instance);
		foreach (Vector2Int position in positions)
		{
			_posToBlock.Add(position, instance);
		}

		_blockToPos.Add(instance, positions.ToArray());

		GameObject go = Instantiate(block, transform);
		go.transform.localPosition = _grid.GetCellCenterLocal(new Vector3Int(rootLocation.x, rootLocation.y, 0));
		go.transform.localRotation = RotationUtils.GetPhysicalRotation(_rotation);

		_blockToObject.Add(instance, go);
	}

	private void RemoveBlock(Vector2Int location)
	{
		if (!_posToBlock.TryGetValue(location, out VehicleBlueprint.BlockInstance instance))
		{
			throw new EmptyBlockError();
		}

		var blockTemplate = BlockRegistry.Instance.GetBlock(instance.BlockID);
		if (!blockTemplate.GetComponent<BlockInfo>().AllowErase)
		{
			throw new BlockNotErasable();
		}

		Vector2Int[] positions = _blockToPos[instance];

		foreach (Vector2Int position in positions)
		{
			_posToBlock.Remove(position);
		}

		_blockToPos.Remove(instance);

		_blueprint.Blocks.Remove(instance);

		GameObject go = _blockToObject[instance];
		Destroy(go);
		_blockToObject.Remove(instance);
	}

	#endregion

	#region Vehicle Management

	private void ClearAll()
	{
		foreach (VehicleBlueprint.BlockInstance instance in _blueprint.Blocks)
		{
			GameObject block = _blockToObject[instance];
			Destroy(block);
		}

		_blueprint = new VehicleBlueprint();
		_posToBlock.Clear();
		_blockToPos.Clear();
		_blockToObject.Clear();
	}

	private void InitVehicle()
	{
		AddBlock(ControlCoreBlock, Vector2Int.zero);
	}

	public VehicleBlueprint SaveVehicle()
	{
		return _blueprint;
	}

	public void LoadVehicle(VehicleBlueprint blueprint)
	{
		ClearAll();
		_blueprint = blueprint;

		foreach (VehicleBlueprint.BlockInstance instance in _blueprint.Blocks)
		{
			GameObject blockPrefab = BlockRegistry.Instance.GetBlock(instance.BlockID);

			var info = blockPrefab.GetComponent<BlockInfo>();

			var positions = new List<Vector2Int>();

			foreach (Vector3Int localPosition in info.Bounds.allPositionsWithin)
			{
				Vector2Int globalPosition = new Vector2Int(instance.X, instance.Y)
				                            + RotationUtils.RotatePoint(localPosition, instance.Rotation);
				positions.Add(globalPosition);
				_posToBlock.Add(globalPosition, instance);
			}

			_blockToPos.Add(instance, positions.ToArray());

			GameObject go = Instantiate(blockPrefab, transform);
			go.transform.localPosition = _grid.GetCellCenterLocal(new Vector3Int(instance.X, instance.Y, 0));
			go.transform.localRotation = RotationUtils.GetPhysicalRotation(instance.Rotation);

			_blockToObject.Add(instance, go);
		}
	}

	#endregion
}