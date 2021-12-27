using Syy1125.OberthEffect.Spec.Checksum;

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

public struct ControlGroupSpec
{
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