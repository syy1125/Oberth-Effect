using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Unity;
using Syy1125.OberthEffect.Spec.Yaml;
using UnityEngine;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;

namespace Syy1125.OberthEffect.Spec
{
internal class InvalidSpecStructureException : Exception
{}

public static class ModLoader
{
	[Serializable]
	public struct ModListElement
	{
		// Folder name only, not full path
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
	public static bool Initialized { get; private set; }

	public static void Init()
	{
		_modsRoot = Path.Combine(Application.streamingAssetsPath, "Mods");
		Initialized = true;
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

	private static void SaveModList(List<ModListElement> modList)
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

	#region Mod Content

	private static Dictionary<string, GameSpecDocument> _blockDocuments;
	internal static IReadOnlyList<SpecInstance<BlockSpec>> AllBlocks;

	private static Dictionary<string, GameSpecDocument> _textureDocuments;
	internal static IReadOnlyList<SpecInstance<TextureSpec>> AllTextures;

	private static Dictionary<string, GameSpecDocument> _vehicleResourceDocuments;
	internal static IReadOnlyList<SpecInstance<VehicleResourceSpec>> AllVehicleResources;

	private static Dictionary<string, GameSpecDocument> _controlGroupDocuments;
	internal static IReadOnlyList<SpecInstance<ControlGroupSpec>> AllControlGroups;

	private static Dictionary<string, GameSpecDocument> _blockCategoryDocuments;
	internal static IReadOnlyList<SpecInstance<BlockCategorySpec>> AllBlockCategories;

	public static ushort BasicChecksum { get; private set; }
	public static ushort StrictChecksum { get; private set; }
	public static ushort FullChecksum { get; private set; }

	public static bool DataReady { get; private set; }

	private class GameSpecDocument
	{
		public YamlDocument SpecDocument;
		public List<string> OverrideOrder;
	}

	#endregion

	public static void LoadAllEnabledContent()
	{
		LoadDocuments();
		ParseDocuments();
		ValidateData();
		DataReady = true;
	}

	#region Load Documents

	private static void LoadDocuments()
	{
		_blockDocuments = new Dictionary<string, GameSpecDocument>();
		_textureDocuments = new Dictionary<string, GameSpecDocument>();
		_vehicleResourceDocuments = new Dictionary<string, GameSpecDocument>();
		_controlGroupDocuments = new Dictionary<string, GameSpecDocument>();
		_blockCategoryDocuments = new Dictionary<string, GameSpecDocument>();

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

			LoadModContent(
				mod, "Blocks", null,
				nameof(BlockSpec.BlockId), _blockDocuments
			);

			LoadModContent(
				mod, "Block Categories", null,
				nameof(BlockCategorySpec.BlockCategoryId), _blockCategoryDocuments
			);

			LoadModContent(
				mod, "Textures",
				(filePath, document) =>
				{
					var mappingNode = (YamlMappingNode) document.RootNode;

					if (mappingNode.Children.TryGetValue(nameof(TextureSpec.ImagePath), out YamlNode node))
					{
						mappingNode.Children[nameof(TextureSpec.ImagePath)] = new YamlScalarNode(
							Path.Combine(
								Path.GetDirectoryName(filePath) ?? throw new ArgumentException(),
								((YamlScalarNode) node).Value
							)
						);
					}
				},
				nameof(TextureSpec.TextureId), _textureDocuments
			);

			LoadModContent(
				mod, "Vehicle Resources", null,
				nameof(VehicleResourceSpec.ResourceId), _vehicleResourceDocuments
			);

			LoadModContent(
				mod, "Control Groups", null,
				nameof(ControlGroupSpec.ControlGroupId), _controlGroupDocuments
			);
		}
	}

	private static void LoadModContent(
		ModListElement mod, string subDirectory, Action<string, YamlDocument> preprocess,
		string idKey, IDictionary<string, GameSpecDocument> result
	)
	{
		string contentRoot = Path.Combine(_modsRoot, mod.Directory, subDirectory);
		if (!Directory.Exists(contentRoot)) return;

		string[] files = Directory.EnumerateFiles(contentRoot, "*.yaml", SearchOption.AllDirectories).ToArray();

		foreach (string file in files)
		{
			StreamReader reader = File.OpenText(file);

			// Each file should have strong exception guarantee
			// Either all documents in the file load and merge successfully, or none of them do.

			try
			{
				YamlStream yaml = new YamlStream();
				yaml.Load(reader);

				var altered = new Dictionary<string, YamlDocument>();

				foreach (YamlDocument document in yaml.Documents)
				{
					preprocess?.Invoke(file, document);

					try
					{
						string id = ((YamlScalarNode) document.RootNode[idKey]).Value;

						if (altered.TryGetValue(id, out YamlDocument current))
						{
							if (!mod.Mod.AllowDuplicateDefs)
							{
								Debug.LogError(
									string.Join(
										"\n",
										$"Mod {mod.Mod.DisplayName} contains multiple definitions for {subDirectory} \"{id}\".",
										"This is most likely an issue with the mod, but we will attempt to merge the definitions together anyway.",
										$"To disable this warning, set \"{nameof(ModSpec.AllowDuplicateDefs)}\" to true in mod.json."
									)
								);
							}

							altered[id] = YamlMergeHelper.DeepMerge(current, document);
						}
						else if (result.TryGetValue(id, out GameSpecDocument original))
						{
							altered.Add(id, YamlMergeHelper.DeepMerge(original.SpecDocument, document));
						}
						else
						{
							altered.Add(id, YamlMergeHelper.DeepCopy(document));
						}
					}
					catch (InvalidCastException)
					{
						throw new InvalidSpecStructureException();
					}
					catch (YamlMergeException)
					{
						throw new InvalidSpecStructureException();
					}
				}

				// At this point, merge attempts are all successful, and we shouldn't see any more exceptions popping up.
				// (We can't really do anything anyway if dictionary operations and assignment operations are failing)
				foreach (KeyValuePair<string, YamlDocument> entry in altered)
				{
					if (result.TryGetValue(entry.Key, out GameSpecDocument original))
					{
						original.SpecDocument = entry.Value;
						original.OverrideOrder.Add(mod.Mod.DisplayName);
					}
					else
					{
						result.Add(
							entry.Key, new GameSpecDocument
							{
								SpecDocument = entry.Value,
								OverrideOrder = new List<string>(new[] { mod.Mod.DisplayName })
							}
						);
					}
				}
			}
			catch (YamlException)
			{
				Debug.LogError($"Failed to parse yaml from {file}, skipping. This is most likely a mod problem.");
			}
			catch (InvalidSpecStructureException)
			{
				Debug.LogError($"Invalid spec structure in {file}, skipping. This is most likely a mod problem.");
			}
			catch (Exception)
			{
				Debug.Log(
					$"Unexpected error when loading file {file}. This is a problem with mod loading system!"
				);
			}
			finally
			{
				reader.Dispose();
			}
		}
	}

	#endregion

	#region Parse Documents

	private static void ParseDocuments()
	{
		var deserializer = new DeserializerBuilder()
			.WithTypeConverter(new Vector2TypeConverter())
			.WithTypeConverter(new Vector2IntTypeConverter())
			.WithObjectFactory(new TextureSpecFactory(new BlockSpecFactory(new DefaultObjectFactory())))
			.Build();

		AllBlocks = ParseSpecInstance<BlockSpec>(deserializer, _blockDocuments.Values);
		AllBlockCategories = ParseSpecInstance<BlockCategorySpec>(deserializer, _blockCategoryDocuments.Values);
		AllTextures = ParseSpecInstance<TextureSpec>(deserializer, _textureDocuments.Values);
		AllVehicleResources = ParseSpecInstance<VehicleResourceSpec>(deserializer, _vehicleResourceDocuments.Values);
		AllControlGroups = ParseSpecInstance<ControlGroupSpec>(deserializer, _controlGroupDocuments.Values);
	}

	private static IReadOnlyList<SpecInstance<T>> ParseSpecInstance<T>(
		IDeserializer deserializer, IEnumerable<GameSpecDocument> documents
	)
	{
		List<SpecInstance<T>> instances = new List<SpecInstance<T>>();

		var documentList = documents.ToList();

		for (var i = 0; i < documentList.Count; i++)
		{
			GameSpecDocument document = documentList[i];

			lock (LoadStateLock)
			{
				LoadState = State.ParseDocuments;
				LoadProgress = Tuple.Create(i + 1, documentList.Count);
				LoadDescription = null;
			}

			try
			{
				var spec = deserializer.Deserialize<T>(new YamlStreamParserAdapter(document.SpecDocument.RootNode));
				instances.Add(
					new SpecInstance<T>
					{
						Spec = spec,
						OverrideOrder = document.OverrideOrder
					}
				);
			}
			catch (YamlException e)
			{
				Debug.LogError(
					$"Deserialization error {e.Message} when deserializing document\n{document.SpecDocument}"
				);
				var serializer = new SerializerBuilder().Build();
				Debug.Log(serializer.Serialize(document.SpecDocument));
			}
		}

		return instances;
	}

	#endregion

	#region Validate Documents

	private static void ValidateData()
	{
		lock (LoadStateLock)
		{
			LoadState = State.ValidateDocuments;
			LoadProgress = null;
			LoadDescription = null;
		}

		ValidateBlocks();
		ValidateTextures();
		ValidateVehicleResources();
	}

	private static void ValidateBlocks()
	{
		HashSet<string> textureIds = new HashSet<string>(AllTextures.Select(instance => instance.Spec.TextureId));
		HashSet<string> resourceIds =
			new HashSet<string>(AllVehicleResources.Select(instance => instance.Spec.ResourceId));

		foreach (SpecInstance<BlockSpec> instance in AllBlocks)
		{
			foreach (RendererSpec renderer in instance.Spec.Renderers)
			{
				if (!textureIds.Contains(renderer.TextureId))
				{
					Debug.LogError(
						$"Block {instance.Spec.BlockId} references texture {renderer.TextureId} which does not exist"
					);
				}
			}

			if (instance.Spec.Propulsion?.Engine != null)
			{
				foreach (string resourceId in instance.Spec.Propulsion.Engine.MaxResourceUse.Keys)
				{
					ValidateVehicleResourceId(resourceId, instance.Spec.BlockId, resourceIds);
				}

				if (instance.Spec.Propulsion.Engine.Particles != null)
				{
					foreach (ParticleSystemSpec particle in instance.Spec.Propulsion.Engine.Particles)
					{
						ValidateColor(particle.Color, instance.Spec.BlockId, "ParticleSystem", true);
					}
				}
			}

			if (instance.Spec.Propulsion?.OmniThruster != null)
			{
				foreach (string resourceId in instance.Spec.Propulsion.OmniThruster.MaxResourceUse.Keys)
				{
					ValidateVehicleResourceId(resourceId, instance.Spec.BlockId, resourceIds);
				}

				if (instance.Spec.Propulsion.OmniThruster.Particles != null)
				{
					foreach (ParticleSystemSpec particle in instance.Spec.Propulsion.OmniThruster.Particles)
					{
						ValidateColor(particle.Color, instance.Spec.BlockId, "ParticleSystem", true);
					}
				}
			}
		}
	}

	private static void ValidateVehicleResourceId(
		string checkResourceId, string blockId, ICollection<string> validResourceIds
	)
	{
		if (!validResourceIds.Contains(checkResourceId))
		{
			Debug.LogError(
				$"Block {blockId} references VehicleResource {checkResourceId} which does not exist"
			);
		}
	}

	private static void ValidateColor(string color, string blockId, string component, bool acceptColorScheme)
	{
		switch (color.ToLower())
		{
			case "primary":
			case "secondary":
			case "tertiary":
				if (!acceptColorScheme)
				{
					Debug.LogError($"{component} in {blockId} does not accept color scheme based color assignments");
				}

				break;
			default:
				// TODO ColorUtility.TryParseHtmlString cannot be called outside main thread
				// if (!ColorUtility.TryParseHtmlString(color, out Color _))
				// {
				// 	Debug.LogError(
				// 		$"{component} in {blockId} uses invalid color {color}"
				// 	);
				// }

				break;
		}
	}

	private static void ValidateTextures()
	{
		foreach (SpecInstance<TextureSpec> instance in AllTextures)
		{
			if (!File.Exists(instance.Spec.ImagePath))
			{
				Debug.LogError(
					$"Texture {instance.Spec.TextureId} references image at {instance.Spec.ImagePath} which does not exist"
				);
			}
		}
	}

	private static void ValidateVehicleResources()
	{
		foreach (SpecInstance<VehicleResourceSpec> instance in AllVehicleResources)
		{
			// TODO ColorUtility.TryParseHtmlString cannot be called outside main thread
			// if (!ColorUtility.TryParseHtmlString(instance.Spec.DisplayColor, out Color _))
			// {
			// 	Debug.LogError(
			// 		$"VehicleResource {instance.Spec.ResourceId} has invalid color {instance.Spec.DisplayColor}"
			// 	);
			// }
		}
	}

	#endregion

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
		ushort blockSpecChecksum = GetChecksum(AllBlocks, level);
		ushort blockCategorySpecChecksum = GetChecksum(AllBlockCategories, level);
		ushort textureSpecChecksum = GetChecksum(AllTextures, level);
		ushort vehicleResourceChecksum = GetChecksum(AllVehicleResources, level);
		ushort controlGroupChecksum = GetChecksum(AllControlGroups, level);

		// Convert throws OverflowException even in unchecked. So start with uint and then truncate it down to ushort.
		return (ushort) Convert.ToUInt32(
			blockSpecChecksum
			+ blockCategorySpecChecksum
			+ textureSpecChecksum
			+ vehicleResourceChecksum
			+ controlGroupChecksum
		);
	}

	private static uint GetChecksum(IEnumerable<GameSpecDocument> values)
	{
		return values
			.AsParallel()
			.Select(document => YamlChecksum.GetChecksum(document.SpecDocument))
			.AsSequential()
			.Aggregate(0u, (sum, item) => sum + item);
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
		return (ushort) ((sum & 0xffff) ^ ((sum >> 8) & 0xffff) ^ ((sum >> 16) & 0xffff) ^ ((sum >> 24) | 0xffff));
	}
}
}