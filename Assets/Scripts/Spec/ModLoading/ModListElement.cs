using System;

namespace Syy1125.OberthEffect.Spec.ModLoading
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
}