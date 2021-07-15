namespace Syy1125.OberthEffect.Utils
{
/// <summary>
/// Central place to store photon custom property keys to avoid potential conflicts
/// </summary>
public static class PropertyKeys
{
	// PlayerPrefs keys
	public const string PLAYER_NAME = "NickName";
	public const string PRIMARY_COLOR = "PrimaryColor";
	public const string SECONDARY_COLOR = "SecondaryColor";
	public const string TERTIARY_COLOR = "TertiaryColor";
	public const string ANALYSIS_USE_ACC_MODE = "AnalysisUseAccelerationMode";

	// Photon room custom properties
	public const string ROOM_NAME = "n";

	// Photon player custom properties
	public const string VEHICLE_NAME = "vn";
	public const string READY = "r";
}
}