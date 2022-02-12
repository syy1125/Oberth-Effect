using System;
using System.Collections.Generic;
using Syy1125.OberthEffect.Foundation.Colors;
using UnityEngine;

namespace Syy1125.OberthEffect.Foundation
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

		public override string ToString()
		{
			return $"{nameof(BlockId)}: {BlockId}, {nameof(Position)}: {Position}, {nameof(Rotation)}: {Rotation}";
		}
	}

	public string Name;
	public string Description;
	public List<BlockInstance> Blocks;
	public bool UseMirror;
	public int MirrorPosition;

	public int CachedCost;

	public bool UseCustomColors;
	public ColorScheme ColorScheme;

	public VehicleControlMode DefaultControlMode;

	public VehicleBlueprint()
	{
		Name = "New Vehicle";
		Description = "";
		Blocks = new List<BlockInstance>();
	}
}
}