﻿using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class VehicleBlueprint
{
	[Serializable]
	public class BlockInstance
	{
		public string BlockID;
		public int X;
		public int Y;
		public int Rotation;
		public string Config;
	}

	public string Name;
	public List<BlockInstance> Blocks;

	public VehicleBlueprint()
	{
		Name = "New Vehicle";
		Blocks = new List<BlockInstance>();
	}
}