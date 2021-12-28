using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

namespace Syy1125.OberthEffect.Init
{
[Serializable]
public struct KeybindOverride
{
	public string ActionId;
	public int BindingIndex;
	public string OverridePath;
}

[Serializable]
public class KeybindProfile
{
	public KeybindOverride[] Overrides;

	public KeybindProfile()
	{
		Overrides = new KeybindOverride[0];
	}
}

public class KeybindManager : MonoBehaviour
{
	public static KeybindManager Instance { get; private set; }

	public InputActionAsset InputActions;

	private KeybindProfile _profile;
	public bool Ready { get; private set; } = true;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else if (Instance != this)
		{
			Destroy(gameObject);
			return;
		}
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public Coroutine LoadKeybinds()
	{
		if (!Ready) throw new Exception("Keybind manager is not ready");
		return StartCoroutine(DoLoadKeybinds());
	}

	private IEnumerator DoLoadKeybinds()
	{
		Ready = false;

		if (_profile == null)
		{
			string profilePath = Path.Combine(Application.persistentDataPath, "KeybindProfile.json");

			if (File.Exists(profilePath))
			{
				Task<string> readTask = Task.Run(() => File.ReadAllText(profilePath));
				yield return new WaitUntil(() => readTask.IsCompleted);

				_profile = JsonUtility.FromJson<KeybindProfile>(readTask.Result);
			}
			else
			{
				_profile = new KeybindProfile();
			}
		}

		Debug.Log($"Loaded {_profile.Overrides.Length} keybind overrides");

		var overrides = new Dictionary<string, Dictionary<int, string>>();
		foreach (KeybindOverride item in _profile.Overrides)
		{
			if (!overrides.TryGetValue(item.ActionId, out var actionOverride))
			{
				actionOverride = new Dictionary<int, string>();
				overrides.Add(item.ActionId, actionOverride);
			}

			actionOverride.Add(item.BindingIndex, item.OverridePath);
		}

		long startTime = Stopwatch.GetTimestamp();
		long timestamp = startTime;
		long timeThreshold = Stopwatch.Frequency / 50; // 20ms
		int frames = 0;

		foreach (InputAction action in InputActions)
		{
			if (overrides.TryGetValue(action.id.ToString(), out var actionOverrides))
			{
				for (int i = 0; i < action.bindings.Count; i++)
				{
					if (actionOverrides.TryGetValue(i, out string overridePath))
					{
						action.ApplyBindingOverride(i, overridePath);
					}
					else if (HasOverride(action.bindings[i]))
					{
						action.RemoveBindingOverride(i);
					}
				}
			}
			else if (HasOverride(action))
			{
				action.RemoveAllBindingOverrides();
			}

			long time = Stopwatch.GetTimestamp();
			if (time - timestamp > timeThreshold)
			{
				timestamp = time;
				frames++;
				yield return null;
			}
		}

		Debug.Log($"Reloading keybind overrides took {frames + 1} frames.");

		Ready = true;
	}

	public Coroutine SaveKeybinds()
	{
		if (!Ready) throw new Exception("Keybind manager is not ready");
		return StartCoroutine(DoSaveKeybinds());
	}

	private IEnumerator DoSaveKeybinds()
	{
		Ready = false;

		List<KeybindOverride> overrides = new List<KeybindOverride>();

		foreach (InputAction action in InputActions)
		{
			for (int i = 0; i < action.bindings.Count; i++)
			{
				if (action.bindings[i].isComposite) continue;

				if (action.bindings[i].overridePath != null)
				{
					overrides.Add(
						new KeybindOverride
						{
							ActionId = action.id.ToString(),
							BindingIndex = i,
							OverridePath = action.bindings[i].overridePath
						}
					);
				}
			}
		}

		Debug.Log($"Saving {overrides.Count} keybind overrides to profile");

		_profile = new KeybindProfile
		{
			Overrides = overrides.ToArray()
		};

		string profilePath = Path.Combine(Application.persistentDataPath, "KeybindProfile.json");
		string content = JsonUtility.ToJson(_profile);

		Task writeTask = Task.Run(() => File.WriteAllText(profilePath, content));
		yield return new WaitUntil(() => writeTask.IsCompleted);

		Ready = true;
	}

	public static bool HasOverride(InputAction action)
	{
		return action.bindings.Any(binding => !binding.isComposite && HasOverride(binding));
	}

	public static bool HasOverride(InputBinding binding)
	{
		return binding.effectivePath != binding.path;
	}
}
}