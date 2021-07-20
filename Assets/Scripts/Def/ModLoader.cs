using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Syy1125.OberthEffect.Def
{
public static class ModLoader
{
	[Serializable]
	public struct ModListElement
	{
		public string Folder;
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

	public static IReadOnlyList<ModListElement> AllMods { get; private set; }


	public static void DiscoverMods()
	{
		_modsRoot = Path.Combine(Application.streamingAssetsPath, "Mods");

		List<string> modFolders = GetValidModFolders();
		List<ModListElement> modList = ReadModList();

		MatchModList(modList, modFolders);

		SaveModList(modList);

		AllMods = modList.AsReadOnly();
	}

	private static List<string> GetValidModFolders()
	{
		List<string> modFolders = new List<string>();

		foreach (string modFolder in Directory.EnumerateDirectories(_modsRoot))
		{
			if (File.Exists(Path.Combine(modFolder, "mod.json")))
			{
				modFolders.Add(Path.GetFileName(modFolder));
			}
			else
			{
				Debug.LogError(
					$"Mod {Path.GetFileName(modFolder)} does not have mod.json and is being skipped. This is most likely a problem with the mod."
				);
			}
		}

		return modFolders;
	}

	private static List<ModListElement> ReadModList()
	{
		string modListPath = Path.Combine(_modsRoot, "modlist.json");

		if (!File.Exists(modListPath)) return new List<ModListElement>();

		string content = File.ReadAllText(modListPath);
		return JsonUtility.FromJson<ModListSpec>(content).ModList;
	}


	private static void MatchModList(IList<ModListElement> modList, IList<string> modFolders)
	{
		for (int i = 0; i < modList.Count;)
		{
			int index = modFolders.IndexOf(modList[i].Folder);

			if (index >= 0)
			{
				// Mod exists and validated
				var modSpec = modList[i];
				modSpec.Mod = LoadModSpec(modList[i].Folder);
				modList[i] = modSpec;

				modFolders.RemoveAt(index);
				i++;
			}
			else
			{
				// Mod no longer exists
				Debug.Log($"Removing mod {modList[i].Folder} from mod list");
				modList.RemoveAt(i);
			}
		}

		foreach (string modFolder in modFolders)
		{
			var modSpec = LoadModSpec(modFolder);
			Debug.Log($"Discovered new mod {modSpec.DisplayName} in {modFolder}");

			modList.Add(
				new ModListElement
				{
					Folder = modFolder,
					Enabled = true,
					Mod = LoadModSpec(modFolder)
				}
			);
		}
	}

	private static ModSpec LoadModSpec(string modFolder)
	{
		string modDefPath = Path.Combine(_modsRoot, modFolder, "mod.json");
		return JsonUtility.FromJson<ModSpec>(File.ReadAllText(modDefPath));
	}

	private static void SaveModList(List<ModListElement> modList)
	{
		string content = JsonUtility.ToJson(new ModListSpec { ModList = modList }, true);
		File.WriteAllText(Path.Combine(_modsRoot, "modlist.json"), content);
	}
}
}