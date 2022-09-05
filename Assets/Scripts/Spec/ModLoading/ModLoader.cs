using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.SchemaGen;
using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;
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
	private class ModDirectoryComparer : IEqualityComparer<ModListElement>
	{
		public bool Equals(ModListElement left, ModListElement right)
		{
			return left.Directory == right.Directory;
		}

		public int GetHashCode(ModListElement element)
		{
			return element.Directory.GetHashCode();
		}
	}

	public enum State
	{
		LoadModList,
		LoadDocuments,
		ParseDocuments,
		ValidateDocuments,
		EmitSchema,
	}

	public static readonly object LoadStateLock = new();
	public static State LoadState;
	public static Tuple<int, int> LoadProgress;
	public static string LoadDescription;

	public static Action OnResetMods;
	public static Action<SerializerBuilder, DeserializerBuilder> OnAugmentSerializer;

	private static string _streamingAssetsModsRoot = null;
	private static string _persistentDataModsRoot = null;

	internal static List<Assembly> ModAssemblies;
	internal static ModLoadingPipeline<BlockSpec> BlockPipeline;
	internal static ModLoadingPipeline<BlockCategorySpec> BlockCategoryPipeline;
	internal static ModLoadingPipeline<DamageTypeSpec> DamageTypePipeline;
	internal static ModLoadingPipeline<ArmorTypeSpec> ArmorTypePipeline;
	internal static ModLoadingPipeline<TextureSpec> TexturePipeline;
	internal static ModLoadingPipeline<SoundSpec> SoundPipeline;
	internal static ModLoadingPipeline<VehicleResourceSpec> VehicleResourcePipeline;
	internal static ModLoadingPipeline<ControlGroupSpec> ControlGroupPipeline;
	internal static ModLoadingPipeline<StockVehicleSpec> StockVehiclePipeline;

	public static ISerializer Serializer { get; private set; }
	public static IDeserializer Deserializer { get; private set; }

	private static IEnumerable<IModLoadingPipeline> Pipelines => new IModLoadingPipeline[]
	{
		BlockPipeline, BlockCategoryPipeline, DamageTypePipeline, ArmorTypePipeline,
		TexturePipeline, SoundPipeline, VehicleResourcePipeline,
		ControlGroupPipeline, StockVehiclePipeline
	};

	public static bool Initialized { get; private set; }

	public static void Init()
	{
		_streamingAssetsModsRoot = Path.Combine(Application.streamingAssetsPath, "Mods");
		_persistentDataModsRoot = Path.Combine(Application.persistentDataPath, "Mods");

		if (!Directory.Exists(_persistentDataModsRoot)) Directory.CreateDirectory(_persistentDataModsRoot);

		ModAssemblies = new();
		BlockPipeline = new("Blocks");
		BlockCategoryPipeline = new("Block Categories");
		DamageTypePipeline = new("Damage Types");
		ArmorTypePipeline = new("Armor Types");
		TexturePipeline = new("Textures");
		SoundPipeline = new("Sounds");
		VehicleResourcePipeline = new("Vehicle Resources");
		ControlGroupPipeline = new("Control Groups");
		StockVehiclePipeline = new("Stock Vehicles");

		Initialized = true;
	}

	public static void ResetMods()
	{
		OnResetMods.Invoke();

		ModAssemblies.Clear();

		foreach (var pipeline in Pipelines)
		{
			pipeline.ResetData();
		}
	}

	public static void InjectBlockSpec(TextAsset blockSpec)
	{
		BlockPipeline.InjectFileContent(blockSpec.text, "Core");
	}

	public static void InjectArmorTypeSpec(TextAsset armorTypeSpec)
	{
		ArmorTypePipeline.InjectFileContent(armorTypeSpec.text, "Core");
	}

	#region Mod List

	public static IReadOnlyList<ModListElement> AllMods { get; private set; }
	public static IEnumerable<ModListElement> EnabledMods => AllMods.Where(mod => mod.Enabled);

	public static void LoadModList()
	{
		lock (LoadStateLock)
		{
			LoadState = State.LoadModList;
			LoadProgress = null;
			LoadDescription = null;
		}

		List<ModListElement> availableMods = GetAvailableMods();
		List<ModListElement> storedModList = ReadStoredModList();

		MatchModList(storedModList, availableMods);

		SaveModList(storedModList);

		AllMods = storedModList.AsReadOnly();
	}

	private static List<ModListElement> GetAvailableMods()
	{
		HashSet<ModListElement> mods = new(new ModDirectoryComparer());

		foreach (string modPath in Directory.EnumerateDirectories(_streamingAssetsModsRoot))
		{
			if (File.Exists(Path.Combine(modPath, "mod.json")))
			{
				mods.Add(new() { Directory = Path.GetFileName(modPath), FullPath = modPath });
			}
			else
			{
				Debug.LogError(
					$"Mod {Path.GetFileName(modPath)} does not have mod.json file and will be skipped."
				);
			}
		}

		foreach (string modPath in Directory.EnumerateDirectories(_persistentDataModsRoot))
		{
			if (File.Exists(Path.Combine(modPath, "mod.json")))
			{
				if (!mods.Add(new() { Directory = Path.GetFileName(modPath), FullPath = modPath }))
				{
					Debug.LogWarning($"Mod \"{Path.GetFileName(modPath)}\" already exists in StreamingAssets.");
				}
			}
			else
			{
				Debug.LogError(
					$"Mod {Path.GetFileName(modPath)} does not have mod.json file and will be skipped."
				);
			}
		}

		return mods.ToList();
	}

	private static List<ModListElement> ReadStoredModList()
	{
		string modListPath = Path.Combine(_persistentDataModsRoot, "modlist.json");
		if (!File.Exists(modListPath)) modListPath = Path.Combine(_streamingAssetsModsRoot, "modlist.json");
		if (!File.Exists(modListPath)) return new();

		string content = File.ReadAllText(modListPath);
		return JsonUtility.FromJson<ModListSpec>(content).ModList;
	}


	private static void MatchModList(IList<ModListElement> storedModList, List<ModListElement> availableMods)
	{
		for (int i = 0; i < storedModList.Count;)
		{
			int index = availableMods.FindIndex(element => element.Directory == storedModList[i].Directory);

			if (index >= 0)
			{
				// Mod exists and validated
				var modSpec = storedModList[i];
				modSpec.FullPath = availableMods[index].FullPath;
				modSpec.Spec = LoadModSpec(modSpec.FullPath);
				storedModList[i] = modSpec;

				availableMods.RemoveAt(index);
				i++;
			}
			else
			{
				// Mod no longer exists
				Debug.Log($"Removing mod {storedModList[i].Directory} from mod list as it no longer exists.");
				storedModList.RemoveAt(i);
			}
		}

		foreach (ModListElement mod in availableMods)
		{
			var modSpec = LoadModSpec(mod.FullPath);
			Debug.Log($"Discovered new mod {modSpec.DisplayName} in {mod.Directory} (IsCodeMod={IsCodeMod(modSpec)})");

			storedModList.Add(
				new()
				{
					Directory = mod.Directory,
					FullPath = mod.FullPath,
					Enabled = !IsCodeMod(modSpec),
					Spec = modSpec
				}
			);
		}
	}

	private static ModSpec LoadModSpec(string modPath)
	{
		string modDefPath = Path.Combine(modPath, "mod.json");
		return JsonUtility.FromJson<ModSpec>(File.ReadAllText(modDefPath));
	}

	private static bool IsCodeMod(ModSpec modSpec)
	{
		return !string.IsNullOrEmpty(modSpec.CodeModPath) && !string.IsNullOrEmpty(modSpec.CodeModEntryPoint);
	}

	public static void SaveModList(List<ModListElement> modList)
	{
		string content = JsonUtility.ToJson(new ModListSpec { ModList = modList }, true);
		File.WriteAllText(Path.Combine(_persistentDataModsRoot, "modlist.json"), content);
	}

	public static bool IsModded()
	{
		bool coreModFound = false;

		foreach (ModListElement element in EnabledMods)
		{
			if (element.Directory == "Oberth Effect")
			{
				coreModFound = true;
			}
			else
			{
				return true;
			}
		}

		return !coreModFound;
	}

	#endregion

	public static ushort BasicChecksum { get; private set; }
	public static ushort StrictChecksum { get; private set; }
	public static ushort FullChecksum { get; private set; }

	public static bool DataReady { get; private set; }

	public static void LoadAllEnabledContent()
	{
		LoadCodeMods();

		LoadDocuments();
		ParseDocuments();
		ValidateData();
		EmitSchema();

		DataReady = true;
	}

	private static void LoadCodeMods()
	{
		foreach (var mod in EnabledMods)
		{
			if (!IsCodeMod(mod.Spec))
			{
				continue;
			}

			Debug.Log($"Loading code mod \"{mod.Spec.DisplayName}\"");

			Assembly modAssembly;
			try
			{
				modAssembly = Assembly.LoadFrom(
					Path.Combine(Application.streamingAssetsPath, "Mods", mod.Directory, mod.Spec.CodeModPath)
				);
			}
			catch (FileNotFoundException)
			{
				Debug.LogError(
					$"Could not find assembly at specified location for code mod \"{mod.Spec.DisplayName}\""
				);
				continue;
			}

			ModAssemblies.Add(modAssembly);

			var entryClass = modAssembly.GetType(mod.Spec.CodeModEntryPoint);

			if (entryClass == null)
			{
				Debug.LogError(
					$"Failed to find entry point class `{mod.Spec.CodeModEntryPoint}` for code mod \"{mod.Spec.DisplayName}\""
				);
				continue;
			}

			var initMethod = entryClass.GetMethod("Init", BindingFlags.Public | BindingFlags.Static);
			initMethod?.Invoke(null, Array.Empty<object>());
		}
	}

	private static void LoadDocuments()
	{
		var enabledMods = EnabledMods.ToList();

		for (var i = 0; i < enabledMods.Count; i++)
		{
			ModListElement mod = enabledMods[i];
			lock (LoadStateLock)
			{
				LoadState = State.LoadDocuments;
				LoadProgress = Tuple.Create(i + 1, enabledMods.Count);
				LoadDescription = enabledMods[i].Spec.DisplayName;
			}

			foreach (IModLoadingPipeline pipeline in Pipelines)
			{
				pipeline.LoadModContent(mod);
			}
		}
	}

	private static void ParseDocuments()
	{
		var serializerBuilder = new SerializerBuilder()
			.WithTypeConverter(new Vector2TypeConverter())
			.WithTypeConverter(new Vector2IntTypeConverter())
			.WithTypeConverter(new ColorTypeConverter());

		var deserializerBuilder = new DeserializerBuilder()
			.WithTypeConverter(new Vector2TypeConverter())
			.WithTypeConverter(new Vector2IntTypeConverter())
			.WithObjectFactory(new TextureSpecFactory(new BlockSpecFactory(new DefaultObjectFactory())));

		OnAugmentSerializer?.Invoke(serializerBuilder, deserializerBuilder);

		Serializer = serializerBuilder.Build();
		Deserializer = deserializerBuilder.Build();

		foreach (IModLoadingPipeline pipeline in Pipelines)
		{
			pipeline.ParseSpecInstances(OnParseProgress);
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

		ValidateBlockIdAttribute.ValidIds = new(BlockPipeline.GetResultIds<string>(spec => spec.Enabled));
		ValidateBlockCategoryIdAttribute.ValidIds =
			new(BlockCategoryPipeline.GetResultIds<string>(spec => spec.Enabled));
		ValidateArmorTypeIdAttribute.ValidIds = new(ArmorTypePipeline.GetResultIds<string>());
		ValidateDamageTypeIdAttribute.ValidIds = new(DamageTypePipeline.GetResultIds<string>());
		ValidateTextureIdAttribute.ValidIds = new(TexturePipeline.GetResultIds<string>());
		ValidateSoundIdAttribute.ValidIds = new(SoundPipeline.GetResultIds<string>());
		ValidateVehicleResourceIdAttribute.ValidIds = new(VehicleResourcePipeline.GetResultIds<string>());
		ValidateControlGroupIdAttribute.ValidIds = new(ControlGroupPipeline.GetResultIds<string>());

		foreach (IModLoadingPipeline pipeline in Pipelines)
		{
			pipeline.ValidateResults();
		}
	}

	private static void EmitSchema()
	{
		lock (LoadStateLock)
		{
			LoadState = State.EmitSchema;
			LoadProgress = null;
			LoadDescription = "discovering specs";
		}

		List<Type> schemaTypes = new();

		schemaTypes.AddRange(
			Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsDefined(typeof(CreateSchemaFileAttribute)))
		);
		foreach (var modAssembly in ModAssemblies)
		{
			schemaTypes.AddRange(
				modAssembly.GetTypes().Where(type => type.IsDefined(typeof(CreateSchemaFileAttribute)))
			);
		}

		Debug.Log($"Found {schemaTypes.Count} schema types");

		for (int i = 0; i < schemaTypes.Count; i++)
		{
			var schemaType = schemaTypes[i];
			var schemaFileAttribute = schemaType.GetCustomAttribute<CreateSchemaFileAttribute>();

			lock (LoadStateLock)
			{
				LoadProgress = Tuple.Create(i + 1, schemaTypes.Count);
				LoadDescription = $"{schemaFileAttribute.FileName}.json";
			}

			var schemaPath = Path.Combine(
				Application.streamingAssetsPath, "JsonSchema",
				Path.ChangeExtension(schemaFileAttribute.FileName, ".json")!
			);
			var schema = SchemaGenerator.GenerateTopLevelSchema(schemaType);
			File.WriteAllText(schemaPath, JsonConvert.SerializeObject(schema, Formatting.Indented));
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
		ushort damageTypeSpecChecksum = GetChecksum(DamageTypePipeline.Results, level);
		ushort armorTypeSpecChecksum = GetChecksum(ArmorTypePipeline.Results, level);
		ushort textureSpecChecksum = GetChecksum(TexturePipeline.Results, level);
		ushort soundChecksum = GetChecksum(SoundPipeline.Results, level);
		ushort vehicleResourceChecksum = GetChecksum(VehicleResourcePipeline.Results, level);
		ushort controlGroupChecksum = GetChecksum(ControlGroupPipeline.Results, level);
		ushort stockVehicleChecksum = GetChecksum(StockVehiclePipeline.Results, level);

		// Convert throws OverflowException even in unchecked. So start with uint and then truncate it down to ushort.
		return (ushort) Convert.ToUInt32(
			blockSpecChecksum
			+ blockCategorySpecChecksum
			+ damageTypeSpecChecksum
			+ armorTypeSpecChecksum
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