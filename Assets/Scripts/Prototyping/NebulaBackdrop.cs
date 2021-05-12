using System.Collections.Generic;
using UnityEngine;

public class NebulaBackdrop : MonoBehaviour
{
	public float Parallax;
	public float Density;
	public GameObject NebulaTemplate;

	private Transform _parent;
	private Camera _mainCamera;
	private Vector3 _baseScale;
	private float _originalSize;

	private HashSet<Vector3Int> _generated;

	private MaterialPropertyBlock _materialProperty;
	private int _nextSeed;

	private void Awake()
	{
		_parent = transform.parent;
		_mainCamera = Camera.main;
		_baseScale = transform.localScale;


		_generated = new HashSet<Vector3Int>();
		_materialProperty = new MaterialPropertyBlock();
		_nextSeed = 0;

		if (_mainCamera != null)
		{
			_originalSize = _mainCamera.orthographicSize;

			foreach (Vector3Int position in GetCameraBounds(_mainCamera).allPositionsWithin)
			{
				bool spawnNebula = Random.value < Density;

				if (spawnNebula)
				{
					SpawnNebula(position);
				}

				_generated.Add(position);
			}
		}
	}

	private BoundsInt GetCameraBounds(Camera cam)
	{
		Vector2 minPoint = transform.InverseTransformPoint(cam.ViewportToWorldPoint(Vector3.zero));
		Vector2 maxPoint = transform.InverseTransformPoint(cam.ViewportToWorldPoint(Vector3.one));

		var bounds = new BoundsInt();

		bounds.SetMinMax(
			new Vector3Int(Mathf.FloorToInt(minPoint.x) - 1, Mathf.FloorToInt(minPoint.y) - 1, 0),
			new Vector3Int(Mathf.CeilToInt(maxPoint.x) + 1, Mathf.CeilToInt(maxPoint.y) + 1, 1)
		);

		return bounds;
	}

	private void SpawnNebula(Vector3Int position)
	{
		GameObject nebula = Instantiate(NebulaTemplate, transform);
		nebula.SetActive(true);
		nebula.transform.localPosition = new Vector3(position.x + Random.value, position.y + Random.value);

		var spriteRenderer = nebula.GetComponent<SpriteRenderer>();
		spriteRenderer.GetPropertyBlock(_materialProperty);
		_materialProperty.SetFloat("_NoiseOffset", _nextSeed++);
		spriteRenderer.SetPropertyBlock(_materialProperty);

		nebula.transform.localScale *= Random.Range(0.5f, 1.5f);
		nebula.SetActive(true);
	}

	private void Update()
	{
		Vector3 offset = -Parallax * _parent.position;
		offset.z = transform.localPosition.z;
		transform.localPosition = offset;

		if (_mainCamera != null)
		{
			transform.localScale =
				_baseScale * Mathf.Lerp(1, _mainCamera.orthographicSize / _originalSize, 1f - Parallax);

			foreach (Vector3Int position in GetCameraBounds(_mainCamera).allPositionsWithin)
			{
				if (_generated.Contains(position)) continue;

				bool spawnNebula = Random.value < Density;

				if (spawnNebula)
				{
					SpawnNebula(position);
				}

				_generated.Add(position);
			}
		}
	}
}