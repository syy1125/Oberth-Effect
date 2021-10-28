namespace Syy1125.OberthEffect.Spec.ControlGroup
{
public struct ControlGroupState
{
	public string StateId;
	public string DisplayName;
	public string DisplayColor;
}

public struct ControlGroupSpec
{
	public string ControlGroupId;
	public string DisplayName;
	public ControlGroupState[] States;
	public string DefaultKeybind;
}
}