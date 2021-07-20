using Syy1125.OberthEffect.Spec;
using UnityEngine;

namespace Syy1125.OberthEffect.Prototyping
{
public class ModLoaderTest : MonoBehaviour
{
	private void Start()
	{
		ModLoader.DiscoverMods();
		Debug.Log(ModLoader.AllMods.Count);
		foreach (ModLoader.ModListElement element in ModLoader.AllMods)
		{
			Debug.Log(
				$"{element.Folder} {element.Enabled} / {element.Mod.DisplayName} {element.Mod.Version} {element.Mod.Description}"
			);
		}
	}
}
}