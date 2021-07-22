using System.IO;
using Syy1125.OberthEffect.Spec;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Yaml;
using UnityEngine;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;

namespace Syy1125.OberthEffect.Prototyping
{
public class ModLoaderTest : MonoBehaviour
{
	private void Start()
	{
		ModLoader.Init();

		ModLoader.LoadModList();
		Debug.Log(ModLoader.AllMods.Count);

		foreach (ModLoader.ModListElement element in ModLoader.AllMods)
		{
			Debug.Log(
				$"{element.Directory} {element.Enabled} / {element.Mod.DisplayName} {element.Mod.Version} {element.Mod.Description}"
			);
		}

		ModLoader.LoadAllEnabledContent();

		Debug.Log($"Block count {ModLoader.AllBlocks.Count} texture count {ModLoader.AllTextures.Count}");
	}
}
}