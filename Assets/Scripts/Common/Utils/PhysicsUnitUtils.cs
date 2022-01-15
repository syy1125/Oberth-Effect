using System;
using System.Collections.Generic;
using UnityEngine;

namespace Syy1125.OberthEffect.Common.Utils
{
public enum PhysicsUnitMode
{
	Game,
	Metric
}

public static class PhysicsUnitUtils
{
	public static PhysicsUnitMode UnitMode { get; private set; }

	public const float METERS_PER_UNIT_LENGTH = 10f;
	public const float GRAMS_PER_UNIT_MASS = 1e6f;
	public const float NEWTONS_PER_UNIT_FORCE = 1e4f;
	public const float GM2_PER_UNIT_MOMENT = 1e5f;
	public const float KNM_PER_UNIT_TORQUE = 100f;

	private static readonly List<Tuple<float, string>> MetricPrefixes = new List<Tuple<float, string>>
	{
		Tuple.Create(1e-3f, "m"),
		Tuple.Create(1f, ""),
		Tuple.Create(1e3f, "k"),
		Tuple.Create(1e6f, "M"),
		Tuple.Create(1e9f, "G"),
	};

	private const float UNIT_TOLERANCE = 0.999f;

	static PhysicsUnitUtils()
	{
		UnitMode = (PhysicsUnitMode) PlayerPrefs.GetInt(PropertyKeys.PHYSICS_UNIT_MODE, 1);
	}

	public static void SetPhysicsUnitMode(PhysicsUnitMode unitMode)
	{
		UnitMode = unitMode;
		PlayerPrefs.SetInt(PropertyKeys.PHYSICS_UNIT_MODE, (int) unitMode);
	}

	private static string FormatValue(float value, string format, string baseUnit)
	{
		(float unit, string prefix) = MetricPrefixes.FindLast(item => value / item.Item1 >= UNIT_TOLERANCE);
		return $"{(value / unit).ToString(format)}{prefix}{baseUnit}";
	}

	public static string FormatLength(float length, string format = "0.#")
	{
		return UnitMode switch
		{
			PhysicsUnitMode.Game => length.ToString(format) + "u",
			PhysicsUnitMode.Metric => (length * METERS_PER_UNIT_LENGTH).ToString(format) + "m",
			_ => throw new ArgumentOutOfRangeException()
		};
	}
	
	public static string FormatDistance(float distance, string format = "G3")
	{
		return UnitMode switch
		{
			PhysicsUnitMode.Game => FormatValue(distance, format, "u"),
			PhysicsUnitMode.Metric => FormatValue(distance * METERS_PER_UNIT_LENGTH, format, "m"),
			_ => throw new ArgumentOutOfRangeException()
		};
	}

	public static string FormatSpeed(float speed, string format = "0.#")
	{
		return UnitMode switch
		{
			PhysicsUnitMode.Game => FormatValue(speed, format, "u/s"),
			PhysicsUnitMode.Metric => FormatValue(speed * METERS_PER_UNIT_LENGTH, format, "m/s"),
			_ => throw new ArgumentOutOfRangeException()
		};
	}

	public static string FormatAcceleration(float acceleration, string format = "0.#")
	{
		return UnitMode switch
		{
			PhysicsUnitMode.Game => FormatValue(acceleration, format, "u/s²"),
			PhysicsUnitMode.Metric => FormatValue(acceleration * METERS_PER_UNIT_LENGTH, format, "m/s²"),
			_ => throw new ArgumentOutOfRangeException()
		};
	}

	public static string FormatAccelerationNumeric(float acceleration, string format = "0.#")
	{
		return UnitMode switch
		{
			PhysicsUnitMode.Game => acceleration.ToString(format),
			PhysicsUnitMode.Metric => (acceleration * METERS_PER_UNIT_LENGTH).ToString(format),
			_ => throw new ArgumentOutOfRangeException()
		};
	}

	public static string GetAccelerationUnits()
	{
		return UnitMode switch
		{
			PhysicsUnitMode.Game => "u/s²",
			PhysicsUnitMode.Metric => "m/s²",
			_ => throw new ArgumentOutOfRangeException()
		};
	}

	public static string FormatMass(float mass, string format = "0.#")
	{
		return UnitMode switch
		{
			PhysicsUnitMode.Game => FormatValue(mass, format, "u"),
			PhysicsUnitMode.Metric => FormatValue(mass * GRAMS_PER_UNIT_MASS, format, "g"),
			_ => throw new ArgumentOutOfRangeException()
		};
	}

	public static string FormatForce(float force, string format = "0.#")
	{
		return UnitMode switch
		{
			PhysicsUnitMode.Game => FormatValue(force, format, "u"),
			PhysicsUnitMode.Metric => FormatValue(force * NEWTONS_PER_UNIT_FORCE, format, "N"),
			_ => throw new ArgumentOutOfRangeException()
		};
	}

	public static string FormatForceNumeric(float force, string format = "0.#")
	{
		return UnitMode switch
		{
			PhysicsUnitMode.Game => force.ToString(format),
			PhysicsUnitMode.Metric => (force * NEWTONS_PER_UNIT_FORCE / 1000f).ToString(format),
			_ => throw new ArgumentOutOfRangeException()
		};
	}

	public static string GetForceUnits()
	{
		return UnitMode switch
		{
			PhysicsUnitMode.Game => "u",
			PhysicsUnitMode.Metric => "kN",
			_ => throw new ArgumentOutOfRangeException()
		};
	}

	public static string FormatImpulse(float impulse, string format = "0.#")
	{
		return UnitMode switch
		{
			PhysicsUnitMode.Game => FormatValue(impulse, format, "u"),
			PhysicsUnitMode.Metric => FormatValue(impulse * NEWTONS_PER_UNIT_FORCE, format, "Ns"),
			_ => throw new ArgumentOutOfRangeException()
		};
	}

	public static string FormatMomentOfInertia(float moment, string format = "#,0.#")
	{
		return UnitMode switch
		{
			PhysicsUnitMode.Game => FormatValue(moment, format, "u"),
			PhysicsUnitMode.Metric => FormatValue(moment * GM2_PER_UNIT_MOMENT, format, "gm²"),
			_ => throw new ArgumentOutOfRangeException()
		};
	}

	public static string FormatTorqueNumeric(float torque, string format = "#,0.#")
	{
		return UnitMode switch
		{
			PhysicsUnitMode.Game => torque.ToString(format),
			PhysicsUnitMode.Metric => (torque * KNM_PER_UNIT_TORQUE).ToString(format),
			_ => throw new ArgumentOutOfRangeException()
		};
	}

	public static string GetTorqueUnits()
	{
		return UnitMode switch
		{
			PhysicsUnitMode.Game => "u",
			PhysicsUnitMode.Metric => "kNm",
			_ => throw new ArgumentOutOfRangeException()
		};
	}
}
}