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
	private Dictionary<string, int> _disabledMaps;

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

		_disabledMaps = new Dictionary<string, int>();

		HashSet<string> mapNames = new HashSet<string>(EnabledMaps);

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

	public void AddDisabledMaps(IEnumerable<string> maps)
	{
		foreach (string mapName in maps.Distinct().Intersect(EnabledMaps))
		{
			if (_disabledMaps.TryGetValue(mapName, out int count))
			{
				_disabledMaps[mapName] = count + 1;
			}
			else
			{
				_disabledMaps.Add(mapName, 1);
				ActionsAsset.FindActionMap(mapName, true).Disable();
			}
		}
	}

	public void RemoveDisabledMaps(IEnumerable<string> maps)
	{
		foreach (string mapName in maps.Distinct().Intersect(EnabledMaps))
		{
			int count = _disabledMaps[mapName];
			if (count > 1)
			{
				_disabledMaps[mapName] = count - 1;
			}
			else
			{
				_disabledMaps.Remove(mapName);
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