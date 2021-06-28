using System;
using System.Collections.Generic;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Simulation.Vehicle;
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
		CycleModeAction.action.Enable();
		CycleModeAction.action.performed += CycleDisplayMode;
	}

	private void OnDisable()
	{
		CycleModeAction.action.performed -= CycleDisplayMode;
		CycleModeAction.action.Disable();
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

	public void SetTarget(VehicleCore target)
	{
		if (_target != null)
		{
			Debug.LogError($"BlockHealthBarControl cannot switch targets!");
			return;
		}

		_target = target;

		if (_target.Loaded)
		{
			SpawnHealthBars();
			UpdateAllHealthBars();
		}
		else
		{
			_target.OnVehicleLoaded.AddListener(HandleVehicleLoad);
		}
	}

	private void HandleVehicleLoad()
	{
		SpawnHealthBars();
		UpdateAllHealthBars();

		_target.OnVehicleLoaded.RemoveListener(HandleVehicleLoad);
	}

	private void SpawnHealthBars()
	{
		Transform t = transform;

		foreach (GameObject block in _target.GetAllBlocks())
		{
			GameObject healthBar = Instantiate(HealthBarPrefab, t);
			healthBar.GetComponent<BlockHealthBar>().Target = block.GetComponent<BlockCore>();

			_healthBars.Add(new Tuple<GameObject, GameObject>(block, healthBar));
		}
	}

	private void Update()
	{
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
					healthBar.SetActive(block.activeSelf && block.GetComponent<BlockCore>().IsDamaged);
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
						block == _hoverBlock && block.activeSelf && block.GetComponent<BlockCore>().IsDamaged
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
		Vector3 mouseWorldPosition = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
		Vector3 blockPosition = _target.transform.InverseTransformPoint(mouseWorldPosition);
		Vector2Int hoverLocation = new Vector2Int(
			Mathf.RoundToInt(blockPosition.x), Mathf.RoundToInt(blockPosition.y)
		);
		return _target.GetBlockAt(hoverLocation);
	}
}
}