using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.ModLoading;
using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;

namespace Syy1125.OberthEffect.Spec.ControlGroup
{
public struct ControlGroupState
{
	public string StateId;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public string DisplayName;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public string DisplayColor;
}

[CreateSchemaFile("ControlGroupSpecSchema")]
public struct ControlGroupSpec
{
	[IdField]
	public string ControlGroupId;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public string DisplayName;
	[RequireChecksumLevel(ChecksumLevel.Everything)]
	public string DefaultKeybind;
	[RequireChecksumLevel(ChecksumLevel.Everything)]
	public string KeybindDescription;
	public ControlGroupState[] States;
}
}