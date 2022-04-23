using System;
using System.Collections.Generic;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Simulation.Construct;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
public class BlockHealthBarControl : MonoBehaviour
{
	public enum HealthBarDisplayMode
	{
		Always,
		IfDamaged,
		OnHover,
		OnHoverIfDamaged,
		Never
	}

	[Header("Inputs")]
	public InputActionReference LookAction;
	public InputActionReference CycleModeAction;

	[Header("Prefabs")]
	public GameObject HealthBarPrefab;

	public HealthBarDisplayMode DisplayMode { get; private set; }
	[Header("Events")]
	public UnityEvent DisplayModeChanged;

	private Camera _mainCamera;
	private VehicleCore _target;
	private List<Tuple<GameObject, GameObject>> _healthBars;

	private GameObject _hoverBlock;

	private void Awake()
	{
		_mainCamera = Camera.main;
		_healthBars = new List<Tuple<GameObject, GameObject>>();

		DisplayMode = HealthBarDisplayMode.IfDamaged;
	}

	private void OnEnable()
	{
		CycleModeAction.action.performed += CycleDisplayMode;
	}

	private void OnDisable()
	{
		CycleModeAction.action.performed -= CycleDisplayMode;
	}

	private void CycleDisplayMode(InputAction.CallbackContext context)
	{
		DisplayMode = DisplayMode switch
		{
			HealthBarDisplayMode.Always => HealthBarDisplayMode.IfDamaged,
			HealthBarDisplayMode.IfDamaged => HealthBarDisplayMode.OnHover,
			HealthBarDisplayMode.OnHover => HealthBarDisplayMode.OnHoverIfDamaged,
			HealthBarDisplayMode.OnHoverIfDamaged => HealthBarDisplayMode.Never,
			HealthBarDisplayMode.Never => HealthBarDisplayMode.Always,
			_ => throw new ArgumentOutOfRangeException()
		};

		UpdateAllHealthBars();
		DisplayModeChanged.Invoke();
	}

	public void SetTargetVehicle(GameObject target)
	{
		if (_target != null)
		{
			foreach (Tuple<GameObject, GameObject> entry in _healthBars)
			{
				Destroy(entry.Item2);
			}

			_healthBars.Clear();
		}

		_target = target.GetComponent<VehicleCore>();

		if (_target != null)
		{
			_target.AfterLoad(SpawnHealthBars);
		}
	}

	private void SpawnHealthBars()
	{
		foreach (GameObject block in _target.GetComponent<ConstructBlockManager>().GetAllBlocks())
		{
			GameObject healthBar = Instantiate(HealthBarPrefab, transform);
			healthBar.GetComponent<BlockHealthBar>().Target = block.GetComponent<BlockHealth>();

			_healthBars.Add(new Tuple<GameObject, GameObject>(block, healthBar));
		}
	}

	private void Update()
	{
		// Handle scene unload edge case
		if (_mainCamera == null || _target == null) return;

		// If necessary, we could optimize this more, but so far it looks like this doesn't impact performance that much
		UpdateAllHealthBars();
	}

	private void UpdateAllHealthBars()
	{
		_hoverBlock = GetHoverBlock();

		switch (DisplayMode)
		{
			case HealthBarDisplayMode.Always:
				foreach ((GameObject block, GameObject healthBar) in _healthBars)
				{
					healthBar.SetActive(block.activeSelf);
				}

				break;
			case HealthBarDisplayMode.IfDamaged:
				foreach ((GameObject block, GameObject healthBar) in _healthBars)
				{
					healthBar.SetActive(block.activeSelf && block.GetComponent<BlockHealth>().IsDamaged);
				}

				break;
			case HealthBarDisplayMode.OnHover:
				foreach ((GameObject block, GameObject healthBar) in _healthBars)
				{
					healthBar.SetActive(block == _hoverBlock && block.activeSelf);
				}

				break;
			case HealthBarDisplayMode.OnHoverIfDamaged:
				foreach ((GameObject block, GameObject healthBar) in _healthBars)
				{
					healthBar.SetActive(
						block == _hoverBlock && block.activeSelf && block.GetComponent<BlockHealth>().IsDamaged
					);
				}

				break;
			case HealthBarDisplayMode.Never:
				foreach ((GameObject _, GameObject healthBar) in _healthBars)
				{
					healthBar.SetActive(false);
				}

				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	private GameObject GetHoverBlock()
	{
		if (!LookAction.action.enabled) return null;
		Vector3 mouseWorldPosition = _mainCamera.ScreenToWorldPoint(LookAction.action.ReadValue<Vector2>());
		Vector3 blockPosition = _target.transform.InverseTransformPoint(mouseWorldPosition);
		Vector2Int hoverLocation = Vector2Int.RoundToInt(blockPosition);
		return _target.GetComponent<ConstructBlockManager>().GetBlockOccupying(hoverLocation);
	}
}
}