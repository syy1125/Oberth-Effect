using System;
using System.Collections.Generic;
using System.IO;
using Syy1125.OberthEffect.Spec.Block;
using UnityEngine;
using YamlDotNet.RepresentationModel;

namespace Syy1125.OberthEffect.Spec
{
public static class ModLoader
{
	[Serializable]
	public struct ModListElement
	{
		public string Directory;
		public bool Enabled;

		[NonSerialized]
		public ModSpec Mod;
	}

	[Serializable]
	private struct ModListSpec
	{
		public List<ModListElement> ModList;
	}

	private static string _modsRoot;

	public static void Init()
	{
		_modsRoot = Path.Combine(Application.streamingAssetsPath, "Mods");
	}


	#region Mod List

	public static IReadOnlyList<ModListElement> AllMods { get; private set; }

	public static void LoadModList()
	{
		List<string> modDirectories = GetValidModDirectories();
		List<ModListElement> modList = ReadStoredModList();

		MatchModList(modList, modDirectories);

		SaveModList(modList);

		AllMods = modList.AsReadOnly();
	}

	private static List<string> GetValidModDirectories()
	{
		List<string> modDirectories = new List<string>();

		foreach (string modDirectory in Directory.EnumerateDirectories(_modsRoot))
		{
			if (File.Exists(Path.Combine(modDirectory, "mod.json")))
			{
				modDirectories.Add(Path.GetFileName(modDirectory));
			}
			else
			{
				Debug.LogError(
					$"Mod {Path.GetFileName(modDirectory)} does not have mod.json and is being skipped. This is most likely a problem with the mod."
				);
			}
		}

		return modDirectories;
	}

	private static List<ModListElement> ReadStoredModList()
	{
		string modListPath = Path.Combine(_modsRoot, "modlist.json");

		if (!File.Exists(modListPath)) return new List<ModListElement>();

		string content = File.ReadAllText(modListPath);
		return JsonUtility.FromJson<ModListSpec>(content).ModList;
	}


	private static void MatchModList(IList<ModListElement> modList, IList<string> modDirectories)
	{
		for (int i = 0; i < modList.Count;)
		{
			int index = modDirectories.IndexOf(modList[i].Directory);

			if (index >= 0)
			{
				// Mod exists and validated
				var modSpec = modList[i];
				modSpec.Mod = LoadModSpec(modList[i].Directory);
				modList[i] = modSpec;

				modDirectories.RemoveAt(index);
				i++;
			}
			else
			{
				// Mod no longer exists
				Debug.Log($"Removing mod {modList[i].Directory} from mod list");
				modList.RemoveAt(i);
			}
		}

		foreach (string modDirectory in modDirectories)
		{
			var modSpec = LoadModSpec(modDirectory);
			Debug.Log($"Discovered new mod {modSpec.DisplayName} in {modDirectory}");

			modList.Add(
				new ModListElement
				{
					Directory = modDirectory,
					Enabled = true,
					Mod = LoadModSpec(modDirectory)
				}
			);
		}
	}

	private static ModSpec LoadModSpec(string modDirectory)
	{
		string modDefPath = Path.Combine(_modsRoot, modDirectory, "mod.json");
		return JsonUtility.FromJson<ModSpec>(File.ReadAllText(modDefPath));
	}

	private static void SaveModList(List<ModListElement> modList)
	{
		string content = JsonUtility.ToJson(new ModListSpec { ModList = modList }, true);
		File.WriteAllText(Path.Combine(_modsRoot, "modlist.json"), content);
	}

	#endregion
}
}