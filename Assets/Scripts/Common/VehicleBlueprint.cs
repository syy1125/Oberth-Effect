using System;
using System.Collections.Generic;
using UnityEngine;

namespace Syy1125.OberthEffect.Common
{
[Serializable]
public class VehicleBlueprint
{
	[Serializable]
	public class BlockInstance
	{
		public string BlockId;
		public Vector2Int Position;
		public int Rotation;
		public string Config;
	}

	public string Name;
	public string Description;
	public List<BlockInstance> Blocks;

	public int CachedCost;

	public bool UseCustomColors;
	public ColorScheme.ColorScheme ColorScheme;

	public VehicleControlMode DefaultControlMode;

	public VehicleBlueprint()
	{
		Name = "New Vehicle";
		Description = "";
		Blocks = new List<BlockInstance>();
	}
}
}