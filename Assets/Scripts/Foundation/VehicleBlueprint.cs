using System;
using System.Collections.Generic;
using Syy1125.OberthEffect.Foundation.Colors;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Lib.Pid;
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

	public PidConfig PidConfig = new()
	{
		Response = 5,
		DerivativeTime = 1,
		IntegralTime = 50
	};

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