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
		ModLoader.LoadModList();
		Debug.Log(ModLoader.AllMods.Count);
		foreach (ModLoader.ModListElement element in ModLoader.AllMods)
		{
			Debug.Log(
				$"{element.Directory} {element.Enabled} / {element.Mod.DisplayName} {element.Mod.Version} {element.Mod.Description}"
			);
		}

		string content = File.ReadAllText(
			Path.Combine(
				Application.streamingAssetsPath,
				"Mods", "Oberth Effect", "Blocks", "Structural", "Block1x1.yaml"
			)
		);
		YamlStream yaml = new YamlStream();
		yaml.Load(new StringReader(content));

		var deserializer = new DeserializerBuilder()
			.WithTypeConverter(new Vector2TypeConverter())
			.WithTypeConverter(new Vector2IntTypeConverter())
			.WithObjectFactory(new BlockSpecFactory(new DefaultObjectFactory()))
			.Build();
		var block = deserializer.Deserialize<BlockSpec>(content);
	}
}
}