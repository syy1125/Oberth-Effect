namespace Syy1125.OberthEffect.Foundation
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
	public const string MASTER_VOLUME = "MasterVolume";
	public const string UI_VOLUME = "UIVolume";
	public const string GAME_SFX_VOLUME = "GameSFXVolume";
	public const string BLOCKS_VOLUME = "BlocksVolume";
	public const string PHYSICS_UNIT_MODE = "PhysicsUnitMode";
	public const string DESIGNER_GRID_OPACITY = "DesignerGridOpacity";
	public const string SCREEN_SHAKE_MULTIPLIER = "ScreenShakeMultiplier";

	// Photon room custom properties
	public const string ROOM_NAME = "n";
	public const string GAME_MODE = "gm";
	public const string FRIENDLY_FIRE_MODE = "ff";
	public const string TEAM_COLORS = "tc";
	public const string USE_TEAM_COLORS = "utc";
	public const string COST_LIMIT_OPTION = "clo";
	public const string COST_LIMIT = "cl";
	public const string SHIPYARD_HEALTH_MULTIPLIER = "shm";

	// Photon player custom properties
	public const string VEHICLE_NAME = "vn";
	public const string VEHICLE_COST = "vc";
	public const string PLAYER_READY = "r";
	public const string TEAM_INDEX = "t";
}
}