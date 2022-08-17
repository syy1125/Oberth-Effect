using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Simulation.Input
{
public class ActionMapControl : MonoBehaviour
{
	public static ActionMapControl Instance { get; private set; }

	public InputActionAsset ActionsAsset;
	public string[] EnabledMaps;
	private Dictionary<string, int> _suppressedMaps;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else if (Instance != this)
		{
			Debug.LogError("Multiple `ActionMapControl`s are being instantiated! Destroying the newest one.");
			Destroy(this);
			return;
		}
		
		// UI input maps are consuming WASD key presses meant for player input map.
		// TODO find a better way to avoid UI consuming key presses.
		InputSystem.settings.SetInternalFeatureFlag("DISABLE_SHORTCUT_SUPPORT", true);

		_suppressedMaps = new();

		HashSet<string> mapNames = new(EnabledMaps);

		foreach (InputActionMap map in ActionsAsset.actionMaps)
		{
			if (mapNames.Contains(map.name))
			{
				map.Enable();
			}
			else
			{
				map.Disable();
			}
		}
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void AddSuppressedMaps(IEnumerable<string> maps)
	{
		foreach (string mapName in maps.Distinct().Intersect(EnabledMaps))
		{
			if (_suppressedMaps.TryGetValue(mapName, out int count))
			{
				_suppressedMaps[mapName] = count + 1;
			}
			else
			{
				_suppressedMaps.Add(mapName, 1);
				ActionsAsset.FindActionMap(mapName, true).Disable();
			}
		}
	}

	public void RemoveSuppressedMaps(IEnumerable<string> maps)
	{
		foreach (string mapName in maps.Distinct().Intersect(EnabledMaps))
		{
			int count = _suppressedMaps[mapName];
			if (count > 1)
			{
				_suppressedMaps[mapName] = count - 1;
			}
			else
			{
				_suppressedMaps.Remove(mapName);
				ActionsAsset.FindActionMap(mapName, true).Enable();
			}
		}
	}

	public bool IsActionMapEnabled(string mapName)
	{
		return ActionsAsset.FindActionMap(mapName, true).enabled;
	}
}
}