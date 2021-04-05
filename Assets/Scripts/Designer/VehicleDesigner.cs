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

	[Header("References")]
	public BlockPalette Palette;

	public DesignerAreaMask AreaMask;

	[Header("Input Actions")]
	public InputActionReference RotateAction;

	public InputActionReference ClickAction;
	public InputActionReference ScrollAction;

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

	private Vector2Int? _selectedLocation;
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
		Palette.OnIndexChanged += HandleIndexChange;

		RotateAction.action.performed += HandleRotate;
		ClickAction.action.performed += HandleClick;
	}

	private void OnDisable()
	{
		Palette.OnIndexChanged -= HandleIndexChange;

		RotateAction.action.performed -= HandleRotate;
		ClickAction.action.performed -= HandleClick;
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

	private void Update()
	{
		var scroll = ScrollAction.action.ReadValue<Vector2>();
		if (AreaMask.Hover && Mathf.Abs(scroll.y) > Mathf.Epsilon)
		{
			float zoom = Mathf.Sign(scroll.y);
			transform.localScale *= Mathf.Exp(zoom / 10f);
		}

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
					_preview.transform.rotation = transform.rotation * RotationUtils.GetPhysicalRotation(_rotation);
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

	public string SaveVehicle()
	{
		return JsonUtility.ToJson(_blueprint);
	}
}