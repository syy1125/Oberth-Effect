using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Block.Propulsion;
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

	#region Mod Content

	private static Dictionary<string, GameSpecDocument> _blockDocuments;
	internal static IReadOnlyCollection<SpecInstance<BlockSpec>> AllBlocks;

	private static Dictionary<string, GameSpecDocument> _textureDocuments;
	internal static IReadOnlyCollection<SpecInstance<TextureSpec>> AllTextures;

	private static Dictionary<string, GameSpecDocument> _vehicleResourceDocuments;
	internal static IReadOnlyCollection<SpecInstance<VehicleResourceSpec>> AllVehicleResources;

	public static uint Checksum { get; private set; }

	public static bool DataReady { get; private set; }

	private class GameSpecDocument
	{
		public YamlDocument SpecDocument;
		public List<string> OverrideOrder;
	}

	public static void LoadAllEnabledContent()
	{
		LoadDocuments();
		ParseDocuments();
		ValidateData();
		ComputeChecksum();
		DataReady = true;
	}

	private static void LoadDocuments()
	{
		_blockDocuments = new Dictionary<string, GameSpecDocument>();
		_textureDocuments = new Dictionary<string, GameSpecDocument>();
		_vehicleResourceDocuments = new Dictionary<string, GameSpecDocument>();

		foreach (ModListElement mod in AllMods)
		{
			if (!mod.Enabled) continue;

			LoadModContent(
				mod, "Blocks", null,
				nameof(BlockSpec.BlockId), _blockDocuments
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
		}
	}

	private static void LoadModContent(
		ModListElement mod, string subDirectory, Action<string, YamlDocument> preprocess,
		string idKey, IDictionary<string, GameSpecDocument> result
	)
	{
		string contentRoot = Path.Combine(_modsRoot, mod.Directory, subDirectory);

		foreach (string file in Directory.EnumerateFiles(contentRoot, "*.yaml", SearchOption.AllDirectories))
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

	private static void ParseDocuments()
	{
		var deserializer = new DeserializerBuilder()
			.WithTypeConverter(new Vector2TypeConverter())
			.WithTypeConverter(new Vector2IntTypeConverter())
			.WithObjectFactory(new TextureSpecFactory(new BlockSpecFactory(new DefaultObjectFactory())))
			.Build();

		AllBlocks = ParseSpecInstance<BlockSpec>(deserializer, _blockDocuments.Values);
		AllTextures = ParseSpecInstance<TextureSpec>(deserializer, _textureDocuments.Values);
		AllVehicleResources = ParseSpecInstance<VehicleResourceSpec>(deserializer, _vehicleResourceDocuments.Values);
	}

	private static IReadOnlyCollection<SpecInstance<T>> ParseSpecInstance<T>(
		IDeserializer deserializer, IEnumerable<GameSpecDocument> documents
	)
	{
		return documents
			.Select(
				document => new SpecInstance<T>
				{
					Spec = deserializer.Deserialize<T>(
						new YamlStreamParserAdapter(document.SpecDocument.RootNode)
					),
					OverrideOrder = document.OverrideOrder
				}
			)
			.ToList();
	}

	private static void ValidateData()
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

			if (instance.Spec.Propulsion.Engine != null)
			{
				foreach (string resourceId in instance.Spec.Propulsion.Engine.MaxResourceUse.Keys)
				{
					ValidateResourceId(resourceId, instance.Spec.BlockId, resourceIds);
				}

				if (instance.Spec.Propulsion.Engine.Particles != null)
				{
					foreach (ParticleSystemSpec particle in instance.Spec.Propulsion.Engine.Particles)
					{
						ValidateColor(particle.Color, instance.Spec.BlockId, "ParticleSystem", true);
					}
				}
			}

			if (instance.Spec.Propulsion.OmniThruster != null)
			{
				foreach (string resourceId in instance.Spec.Propulsion.OmniThruster.MaxResourceUse.Keys)
				{
					ValidateResourceId(resourceId, instance.Spec.BlockId, resourceIds);
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

		foreach (SpecInstance<TextureSpec> instance in AllTextures)
		{
			if (!File.Exists(instance.Spec.ImagePath))
			{
				Debug.LogError(
					$"Texture {instance.Spec.TextureId} references image at {instance.Spec.ImagePath} which does not exist"
				);
			}
		}

		foreach (SpecInstance<VehicleResourceSpec> instance in AllVehicleResources)
		{
			if (!ColorUtility.TryParseHtmlString(instance.Spec.DisplayColor, out Color _))
			{
				Debug.LogError(
					$"VehicleResource {instance.Spec.ResourceId} has invalid color {instance.Spec.DisplayColor}"
				);
			}
		}
	}

	private static void ValidateResourceId(string checkResourceId, string blockId, ICollection<string> validResourceIds)
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
				if (!ColorUtility.TryParseHtmlString(color, out Color _))
				{
					Debug.LogError(
						$"{component} in {blockId} uses invalid color {color}"
					);
				}

				break;
		}
	}

	private static void ComputeChecksum()
	{
		unchecked
		{
			uint blockSpecChecksum = GetChecksum(_blockDocuments.Values);
			uint textureSpecChecksum = GetChecksum(_textureDocuments.Values);
			uint vehicleResourceChecksum = GetChecksum(_vehicleResourceDocuments.Values);

			Checksum = blockSpecChecksum + textureSpecChecksum + vehicleResourceChecksum;
		}
	}

	private static uint GetChecksum(IEnumerable<GameSpecDocument> values)
	{
		return values
			.AsParallel()
			.Select(document => YamlChecksum.GetChecksum(document.SpecDocument))
			.AsSequential()
			.Aggregate(0u, (sum, item) => sum + item);
	}

	#endregion
}
}