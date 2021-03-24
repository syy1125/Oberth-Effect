using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Grid))]
public class VehicleDesigner : MonoBehaviour
{
	public BlockPalette Palette;
	public DesignerAreaMask AreaMask;
	public InputActionReference Rotate;

	private Camera _mainCamera;
	private Plane _plane;
	private Grid _grid;

	private int _rotation;

	private GameObject _preview;
	private bool _prevHover;
	private int _prevIndex;
	private Vector3Int? _prevLocation;

	private void Awake()
	{
		_mainCamera = Camera.main;
		_plane = new Plane(Vector3.back, Vector3.zero);
		_grid = GetComponent<Grid>();
	}

	private void OnEnable()
	{
		Rotate.action.performed += HandleRotate;
	}

	private void Update()
	{
		Vector2 mousePosition = Mouse.current.position.ReadValue();
		Ray mouseRay = _mainCamera.ScreenPointToRay(mousePosition);

		Vector3Int? gridLocation = null;
		if (_plane.Raycast(mouseRay, out float enter))
		{
			gridLocation = _grid.WorldToCell(mouseRay.GetPoint(enter));
		}

		if (Palette.SelectedIndex != _prevIndex || gridLocation != _prevLocation || AreaMask.Hover != _prevHover)
		{
			if (Palette.SelectedIndex < 0 || gridLocation == null)
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
					_preview.transform.rotation = Quaternion.AngleAxis(_rotation * 90f, Vector3.forward);
					_preview.SetActive(AreaMask.Hover);

					foreach (SpriteRenderer sprite in _preview.GetComponentsInChildren<SpriteRenderer>())
					{
						Color c = sprite.color;
						c.a *= 0.5f;
						sprite.color = c;
					}
				}

				_preview.transform.position = _grid.GetCellCenterWorld(gridLocation.Value);

				if (AreaMask.Hover != _prevHover)
				{
					_preview.SetActive(AreaMask.Hover);
				}
			}

			_prevIndex = Palette.SelectedIndex;
			_prevLocation = gridLocation;
			_prevHover = AreaMask.Hover;
		}
	}

	private void OnDisable()
	{
		Rotate.action.performed -= HandleRotate;
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
}