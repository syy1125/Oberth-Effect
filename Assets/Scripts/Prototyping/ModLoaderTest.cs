using Syy1125.OberthEffect.Spec;
using UnityEngine;

namespace Syy1125.OberthEffect.Prototyping
{
public class ModLoaderTest : MonoBehaviour
{
	private void Awake()
	{
		if (!ModLoader.DataReady)
		{
			ModLoader.Init();
			ModLoader.LoadModList();
			ModLoader.LoadAllEnabledContent();
		}
	}

	private void Start()
	{
		foreach (ModLoader.ModListElement element in ModLoader.AllMods)
		{
			Debug.Log(
				$"{element.Directory} {element.Enabled} / {element.Mod.DisplayName} {element.Mod.Version} {element.Mod.Description}"
			);
		}

		Debug.Log(ModLoader.AllTextures.Count);

		foreach (ModLoader.SpecInstance<TextureSpec> instance in ModLoader.AllTextures)
		{
			Debug.Log(instance.Spec.Pivot);
		}

		Debug.Log($"Block count {ModLoader.AllBlocks.Count} texture count {ModLoader.AllTextures.Count}");
		Debug.Log($"Game checksum {ModLoader.Checksum:x}");
	}
}
}