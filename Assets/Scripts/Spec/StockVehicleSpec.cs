using System;
using System.Collections.Generic;
using System.IO;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.ModLoading;
using Syy1125.OberthEffect.Spec.Validation;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec
{
public struct StockVehicleSpec : ICustomValidation, ICustomChecksum
{
	[IdField]
	public string VehicleId;
	public bool Enabled;
	[ValidateFilePath]
	public string VehiclePath;

	public void Validate(List<string> path, List<string> errors)
	{
		ValidationHelper.ValidateFields(path, this, errors);

		if (VehiclePath != null && File.Exists(VehiclePath))
		{
			try
			{
				JsonUtility.FromJson<VehicleBlueprint>(File.ReadAllText(VehiclePath));
			}
			catch (ArgumentException)
			{
				path.Add("VehiclePath");
				errors.Add(ValidationHelper.FormatValidationError(path, "should reference a valid vehicle save file"));
				path.RemoveAt(path.Count - 1);
			}
		}
	}

	public void GetBytes(Stream stream, ChecksumLevel level)
	{
		if (!Enabled && level < ChecksumLevel.Everything)
		{
			return;
		}

		ChecksumHelper.GetBytesFromString(stream, VehicleId);
		ChecksumHelper.GetBytesFromPrimitive(stream, Enabled);

		Stream vehicle = File.OpenRead(VehiclePath);
		vehicle.CopyTo(stream);
	}
}
}