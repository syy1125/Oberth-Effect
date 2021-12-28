using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

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
public struct KeybindProfile
{
	public KeybindOverride[] Overrides;
}

public class KeybindManager : MonoBehaviour
{
	public static KeybindManager Instance { get; private set; }

	public InputActionAsset InputActions;

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

	public void LoadKeybinds()
	{
		foreach (InputAction action in InputActions)
		{
			action.RemoveAllBindingOverrides();
		}

		string profilePath = Path.Combine(Application.persistentDataPath, "KeybindProfile.json");

		if (File.Exists(profilePath))
		{
			KeybindProfile profile = JsonUtility.FromJson<KeybindProfile>(File.ReadAllText(profilePath));

			Debug.Log($"Loaded {profile.Overrides.Length} keybind overrides");

			foreach (KeybindOverride item in profile.Overrides)
			{
				InputActions[item.ActionId].ApplyBindingOverride(item.BindingIndex, item.OverridePath);
			}
		}
	}

	public void SaveKeybinds()
	{
		string profilePath = Path.Combine(Application.persistentDataPath, "KeybindProfile.json");

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

		KeybindProfile profile = new KeybindProfile
		{
			Overrides = overrides.ToArray()
		};

		string content = JsonUtility.ToJson(profile);
		File.WriteAllText(profilePath, content);
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