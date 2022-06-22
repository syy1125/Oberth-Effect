using System;

namespace Syy1125.OberthEffect.Spec.ModLoading
{
[Serializable]
public struct ModSpec
{
	public string DisplayName;
	public string Version;
	public string Description;
	public string CodeModPath;
	public string CodeModEntryPoint;
	public bool AllowDuplicateDefs;
}
}