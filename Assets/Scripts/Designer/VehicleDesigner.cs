using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

class DuplicateBlockError : Exception
{
}

class EmptyBlockError : Exception
{
}

[RequireComponent(typeof(Grid))]
public class VehicleDesigner : MonoBehaviour
{
	#region Public Fields

	public BlockPalette Palette;
	public DesignerAreaMask AreaMask;
	public InputActionReference RotateAction;
	public InputActionReference ClickAction;

	#endregion

	#region Private Fields

	private Camera _mainCamera;
	private Plane _plane;
	private Grid _grid;

	private int _rotation;

	private GameObject _preview;
	private bool _prevHover;
	private int _prevIndex;
	private Vector3Int? _prevLocation;

	private VehicleBlueprint _blueprint;
	private Dictionary<Vector2Int, VehicleBlueprint.BlockInstance> _posToBlock;
	private Dictionary<VehicleBlueprint.BlockInstance, Vector2Int[]> _blockToPos;
	private Dictionary<VehicleBlueprint.BlockInstance, GameObject> _blockToObject;

	#endregion

	private void Awake()
	{
		_mainCamera = Camera.main;
		_plane = new Plane(Vector3.back, Vector3.zero);
		_grid = GetComponent<Grid>();

		_blueprint = new VehicleBlueprint();
		_posToBlock = new Dictionary<Vector2Int, VehicleBlueprint.BlockInstance>();
		_blockToPos = new Dictionary<VehicleBlueprint.BlockInstance, Vector2Int[]>();
		_blockToObject = new Dictionary<VehicleBlueprint.BlockInstance, GameObject>();
	}

	private void OnEnable()
	{
		RotateAction.action.performed += HandleRotate;
		ClickAction.action.performed += HandleClick;
	}

	private Vector3Int? GetHoverLocation()
	{
		Vector2 mousePosition = Mouse.current.position.ReadValue();
		Ray mouseRay = _mainCamera.ScreenPointToRay(mousePosition);

		if (_plane.Raycast(mouseRay, out float enter))
		{
			return _grid.WorldToCell(mouseRay.GetPoint(enter));
		}
		else
		{
			return null;
		}
	}

	private Quaternion GetPhysicalLocalRotation()
	{
		return Quaternion.AngleAxis(_rotation * 90f, Vector3.forward);
	}

	private void Update()
	{
		Vector3Int? hoverLocation = GetHoverLocation();
		if (Palette.SelectedIndex != _prevIndex || hoverLocation != _prevLocation || AreaMask.Hover != _prevHover)
		{
			if (Palette.SelectedIndex < 0 || hoverLocation == null)
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
					_preview.transform.rotation = transform.rotation * GetPhysicalLocalRotation();
					_preview.SetActive(AreaMask.Hover);

					foreach (SpriteRenderer sprite in _preview.GetComponentsInChildren<SpriteRenderer>())
					{
						Color c = sprite.color;
						c.a *= 0.5f;
						sprite.color = c;
					}
				}

				_preview.transform.position = _grid.GetCellCenterWorld(hoverLocation.Value);

				if (AreaMask.Hover != _prevHover)
				{
					_preview.SetActive(AreaMask.Hover);
				}
			}

			_prevIndex = Palette.SelectedIndex;
			_prevLocation = hoverLocation;
			_prevHover = AreaMask.Hover;
		}
	}

	private void OnDisable()
	{
		RotateAction.action.performed -= HandleRotate;
		ClickAction.action.performed -= HandleClick;
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

		if (_preview != null)
		{
			_preview.transform.rotation = Quaternion.AngleAxis(_rotation * 90f, Vector3.forward);
		}
	}

	private Vector2Int RotatePoint(Vector3Int position)
	{
		return _rotation switch
		{
			0 => new Vector2Int(position.x, position.y),
			1 => new Vector2Int(-position.y, position.x),
			2 => new Vector2Int(-position.x, -position.y),
			3 => new Vector2Int(position.y, -position.x),
			_ => throw new ArgumentException()
		};
	}

	private void HandleClick(InputAction.CallbackContext context)
	{
		Vector3Int? hoverLocation = GetHoverLocation();
		if (hoverLocation != null && AreaMask.Hover)
		{
			var rootLocation = new Vector2Int(hoverLocation.Value.x, hoverLocation.Value.y);

			if (Palette.SelectedIndex >= 0)
			{
				GameObject block = Palette.GetSelectedBlock();

				try
				{
					AddBlock(block, rootLocation);
				}
				catch (DuplicateBlockError e)
				{
					// TODO
				}
			}
			else
			{
				switch (Palette.SelectedIndex)
				{
					case BlockPalette.DESELECT_INDEX: return;
					case BlockPalette.ERASE_INDEX:
						// TODO
						return;
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
			Vector2Int globalPosition = rootLocation + RotatePoint(localPosition);

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
		go.transform.localRotation = GetPhysicalLocalRotation();

		_blockToObject.Add(instance, go);
	}

	private void RemoveBlock(Vector2Int location)
	{
		if (!_posToBlock.TryGetValue(location, out VehicleBlueprint.BlockInstance instance))
		{
			throw new EmptyBlockError();
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
}