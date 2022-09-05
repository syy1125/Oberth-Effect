using System;
using System.Collections.Generic;

namespace Syy1125.OberthEffect.Spec.ModLoading
{
[Serializable]
public struct ModListSpec
{
	public List<ModListElement> ModList;
}

[Serializable]
public struct ModListElement
{
	// Folder name only, not full path. A mod is uniquely identified by its folder.
	// If the same mod exists in multiple places, the one in StreamingAssets takes precedence.
	public string Directory;
	// Full path of where the mod resides.
	[NonSerialized]
	public string FullPath;
	public bool Enabled;

	[NonSerialized]
	public ModSpec Spec;
}
}