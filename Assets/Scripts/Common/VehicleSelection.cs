using UnityEngine;

namespace Syy1125.OberthEffect.Common
{
public static class VehicleSelection
{
	private static string _serializedVehicle;
	public static VehicleBlueprint SelectedVehicle { get; private set; }

	public static string SerializedVehicle
	{
		get => _serializedVehicle;
		set
		{
			_serializedVehicle = value;
			SelectedVehicle = value == null ? null : JsonUtility.FromJson<VehicleBlueprint>(value);
		}
	}
}
}