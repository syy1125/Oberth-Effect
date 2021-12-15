using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Simulation.Construct;
using UnityEngine;

namespace Syy1125.OberthEffect.Prototyping
{
public class BlockGraphTester : MonoBehaviour
{
	private void Start()
	{
		var graph = new BlockConnectivityGraph(
			new[]
			{
				new VehicleBlueprint.BlockInstance
					{ BlockId = "OberthEffect/ControlCore", Position = Vector2Int.zero, Rotation = 0 },
				new VehicleBlueprint.BlockInstance
					{ BlockId = "OberthEffect/Block1x1", Position = new Vector2Int(1, 0), Rotation = 0 },
				new VehicleBlueprint.BlockInstance
					{ BlockId = "OberthEffect/Block1x1", Position = new Vector2Int(2, 0), Rotation = 0 },
				new VehicleBlueprint.BlockInstance
					{ BlockId = "OberthEffect/Block1x1", Position = new Vector2Int(3, 0), Rotation = 0 },
				new VehicleBlueprint.BlockInstance
					{ BlockId = "OberthEffect/Block1x1", Position = new Vector2Int(4, 0), Rotation = 0 },
				new VehicleBlueprint.BlockInstance
					{ BlockId = "OberthEffect/Block1x1", Position = new Vector2Int(2, 1), Rotation = 0 },
				new VehicleBlueprint.BlockInstance
					{ BlockId = "OberthEffect/Block1x1", Position = new Vector2Int(2, 2), Rotation = 0 },
				new VehicleBlueprint.BlockInstance
					{ BlockId = "OberthEffect/Block1x1", Position = new Vector2Int(1, 1), Rotation = 0 },
			}
		);

		var graphs = graph.SplitOnBlockDestroyed(new Vector2Int(2, 0));
		Debug.Log(graphs.Count);
		Debug.Log(graphs[0].Count);
		Debug.Log(graphs[1].Count);
	}
}
}