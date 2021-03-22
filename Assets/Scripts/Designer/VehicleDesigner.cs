using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Grid))]
public class VehicleDesigner : MonoBehaviour
{
	public BlockPalette Palette;

	private Camera _mainCamera;
	private Plane _plane;
	private Grid _grid;

	private GameObject _preview;
	private int _prevIndex;
	private Vector3Int? _prevLocation;

	private void Awake()
	{
		_mainCamera = Camera.main;
		_plane = new Plane(Vector3.back, Vector3.zero);
		_grid = GetComponent<Grid>();
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

		if (Palette.SelectedIndex != _prevIndex || gridLocation != _prevLocation)
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
				}

				_preview.transform.position = _grid.GetCellCenterWorld(gridLocation.Value);
			}

			_prevIndex = Palette.SelectedIndex;
			_prevLocation = gridLocation;
		}
	}
}