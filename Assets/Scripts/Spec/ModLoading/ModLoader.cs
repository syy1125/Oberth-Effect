﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using Syy1125.OberthEffect.Spec.Yaml;
using UnityEngine;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;

namespace Syy1125.OberthEffect.Spec.ModLoading
{
public static class ModLoader
{
	[Serializable]
	private struct ModListSpec
	{
		public List<ModListElement> ModList;
	}

	public enum State
	{
		LoadModList,
		LoadDocuments,
		ParseDocuments,
		ValidateDocuments
	}

	public static readonly object LoadStateLock = new object();
	public static State LoadState;
	public static Tuple<int, int> LoadProgress;
	public static string LoadDescription;

	private static string _modsRoot = null;

	internal static ModLoadingPipeline<BlockSpec> BlockPipeline;
	internal static ModLoadingPipeline<BlockCategorySpec> BlockCategoryPipeline;
	internal static ModLoadingPipeline<TextureSpec> TexturePipeline;
	internal static ModLoadingPipeline<SoundSpec> SoundPipeline;
	internal static ModLoadingPipeline<VehicleResourceSpec> VehicleResourcePipeline;
	internal static ModLoadingPipeline<ControlGroupSpec> ControlGroupPipeline;
	internal static ModLoadingPipeline<StockVehicleSpec> StockVehiclePipeline;

	private static IEnumerable<IModLoadingPipeline> Pipelines => new IModLoadingPipeline[]
	{
		BlockPipeline, BlockCategoryPipeline, TexturePipeline, SoundPipeline, VehicleResourcePipeline,
		ControlGroupPipeline, StockVehiclePipeline
	};

	public static bool Initialized { get; private set; }

	public static void Init()
	{
		_modsRoot = Path.Combine(Application.streamingAssetsPath, "Mods");

		BlockPipeline = new ModLoadingPipeline<BlockSpec>(_modsRoot, "Blocks");
		BlockCategoryPipeline = new ModLoadingPipeline<BlockCategorySpec>(_modsRoot, "Block Categories");
		TexturePipeline = new ModLoadingPipeline<TextureSpec>(
			_modsRoot, "Textures",
			ResolveAbsolutePaths(nameof(TextureSpec.ImagePath))
		);
		SoundPipeline = new ModLoadingPipeline<SoundSpec>(
			_modsRoot, "Sounds",
			ResolveAbsolutePaths(nameof(SoundSpec.SoundPath))
		);
		VehicleResourcePipeline = new ModLoadingPipeline<VehicleResourceSpec>(_modsRoot, "Vehicle Resources");
		ControlGroupPipeline = new ModLoadingPipeline<ControlGroupSpec>(_modsRoot, "Control Groups");
		StockVehiclePipeline = new ModLoadingPipeline<StockVehicleSpec>(
			_modsRoot, "Stock Vehicles",
			ResolveAbsolutePaths(nameof(StockVehicleSpec.VehiclePath))
		);

		Initialized = true;
	}

	private static Action<string, YamlDocument> ResolveAbsolutePaths(params string[] fields)
	{
		return (filePath, document) =>
		{
			var mappingNode = (YamlMappingNode) document.RootNode;

			foreach (string field in fields)
			{
				if (mappingNode.Children.TryGetValue(field, out YamlNode node))
				{
					mappingNode.Children[field] = Path.Combine(
						Path.GetDirectoryName(filePath) ?? throw new ArgumentException(),
						((YamlScalarNode) node).Value
					);
				}
			}
		};
	}

	public static void InjectBlockSpec(TextAsset blockSpec)
	{
		BlockPipeline.InjectFileContent(blockSpec.text, "Core");
	}

	#region Mod List

	public static IReadOnlyList<ModListElement> AllMods { get; private set; }

	public static void LoadModList()
	{
		lock (LoadStateLock)
		{
			LoadState = State.LoadModList;
			LoadProgress = null;
			LoadDescription = null;
		}

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

	public static void SaveModList(List<ModListElement> modList)
	{
		string content = JsonUtility.ToJson(new ModListSpec { ModList = modList }, true);
		File.WriteAllText(Path.Combine(_modsRoot, "modlist.json"), content);
	}

	public static bool IsModded()
	{
		bool found = false;

		foreach (ModListElement element in AllMods)
		{
			if (element.Enabled)
			{
				if (element.Directory == "Oberth Effect")
				{
					found = true;
				}
				else
				{
					return true;
				}
			}
		}

		return !found;
	}

	#endregion

	public static ushort BasicChecksum { get; private set; }
	public static ushort StrictChecksum { get; private set; }
	public static ushort FullChecksum { get; private set; }

	public static bool DataReady { get; private set; }

	public static void LoadAllEnabledContent()
	{
		LoadDocuments();
		ParseDocuments();
		ValidateData();
		DataReady = true;
	}

	private static void LoadDocuments()
	{
		var enabledMods = AllMods.Where(mod => mod.Enabled).ToList();

		for (var i = 0; i < enabledMods.Count; i++)
		{
			ModListElement mod = enabledMods[i];
			lock (LoadStateLock)
			{
				LoadState = State.LoadDocuments;
				LoadProgress = Tuple.Create(i + 1, enabledMods.Count);
				LoadDescription = enabledMods[i].Mod.DisplayName;
			}

			foreach (IModLoadingPipeline pipeline in Pipelines)
			{
				pipeline.LoadModContent(mod);
			}
		}
	}

	private static void ParseDocuments()
	{
		var deserializer = new DeserializerBuilder()
			.WithTypeConverter(new Vector2TypeConverter())
			.WithTypeConverter(new Vector2IntTypeConverter())
			.WithObjectFactory(new TextureSpecFactory(new BlockSpecFactory(new DefaultObjectFactory())))
			.Build();

		foreach (IModLoadingPipeline pipeline in Pipelines)
		{
			pipeline.ParseSpecInstances(deserializer, OnParseProgress);
		}
	}

	private static void OnParseProgress(string name, int i, int count)
	{
		lock (LoadStateLock)
		{
			LoadState = State.ParseDocuments;
			LoadProgress = Tuple.Create(i + 1, count);
			LoadDescription = name.ToLower();
		}
	}

	private static void ValidateData()
	{
		lock (LoadStateLock)
		{
			LoadState = State.ValidateDocuments;
			LoadProgress = null;
			LoadDescription = null;
		}

		ValidateBlockIdAttribute.ValidIds =
			new HashSet<string>(BlockPipeline.GetResultIds<string>(spec => spec.Enabled));
		ValidateBlockCategoryIdAttribute.ValidIds =
			new HashSet<string>(BlockCategoryPipeline.GetResultIds<string>(spec => spec.Enabled));
		ValidateTextureIdAttribute.ValidIds =
			new HashSet<string>(TexturePipeline.GetResultIds<string>());
		ValidateSoundIdAttribute.ValidIds =
			new HashSet<string>(SoundPipeline.GetResultIds<string>());
		ValidateVehicleResourceIdAttribute.ValidIds =
			new HashSet<string>(VehicleResourcePipeline.GetResultIds<string>());
		ValidateControlGroupIdAttribute.ValidIds =
			new HashSet<string>(ControlGroupPipeline.GetResultIds<string>());

		foreach (IModLoadingPipeline pipeline in Pipelines)
		{
			pipeline.ValidateResults();
		}
	}

	public static void ComputeChecksum()
	{
		unchecked
		{
			BasicChecksum = ComputeChecksumAtLevel(ChecksumLevel.Basic);
			StrictChecksum = ComputeChecksumAtLevel(ChecksumLevel.Strict);
			FullChecksum = ComputeChecksumAtLevel(ChecksumLevel.Everything);
			Debug.Log($"Checksums: basic {BasicChecksum:x4} / strict {StrictChecksum:x4} / full {FullChecksum:x4}");
		}
	}

	private static ushort ComputeChecksumAtLevel(ChecksumLevel level)
	{
		ushort blockSpecChecksum = GetChecksum(BlockPipeline.Results, level);
		ushort blockCategorySpecChecksum = GetChecksum(BlockCategoryPipeline.Results, level);
		ushort textureSpecChecksum = GetChecksum(TexturePipeline.Results, level);
		ushort soundChecksum = GetChecksum(SoundPipeline.Results, level);
		ushort vehicleResourceChecksum = GetChecksum(VehicleResourcePipeline.Results, level);
		ushort controlGroupChecksum = GetChecksum(ControlGroupPipeline.Results, level);
		ushort stockVehicleChecksum = GetChecksum(StockVehiclePipeline.Results, level);

		// Convert throws OverflowException even in unchecked. So start with uint and then truncate it down to ushort.
		return (ushort) Convert.ToUInt32(
			blockSpecChecksum
			+ blockCategorySpecChecksum
			+ textureSpecChecksum
			+ soundChecksum
			+ vehicleResourceChecksum
			+ controlGroupChecksum
			+ stockVehicleChecksum
		);
	}

	private static ushort GetChecksum<T>(IEnumerable<SpecInstance<T>> values, ChecksumLevel level)
	{
		using MD5 md5 = MD5.Create();
		ulong sum = 0;

		foreach (SpecInstance<T> value in values)
		{
			using MemoryStream stream = new MemoryStream();
			ChecksumHelper.GetBytes(stream, value.Spec, level);
			stream.Seek(0, SeekOrigin.Begin);
			byte[] checksum = md5.ComputeHash(stream);

			// The order of the files should not matter, so we need to combine the checksums in an order-independent way.
			// So far, addition, as a simple associative and commutative operation, seems to work well.
			sum += BitConverter.ToUInt64(checksum, 0);
		}

		return CompactChecksum(sum);
	}

	private static ushort CompactChecksum(ulong sum)
	{
		return (ushort) ((sum & 0xffff) ^ ((sum >> 8) & 0xffff) ^ ((sum >> 16) & 0xffff) ^ ((sum >> 24) & 0xffff));
	}
}
}