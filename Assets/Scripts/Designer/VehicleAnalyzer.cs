using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Utils;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Syy1125.OberthEffect.Designer
{
public struct VehicleAnalysisResult
{
	public float Mass;
	public Vector2 CenterOfMass;
	public float MomentOfInertia;
}

public class VehicleAnalyzer : MonoBehaviour
{
	public VehicleDesigner Designer;
	public VehicleBuilder Builder;
	public Text Output;
	public GameObject CenterOfMassIndicator;

	private VehicleAnalysisResult _result;
	private Coroutine _analysisCoroutine;

	private void OnEnable()
	{
		if (Designer.Blueprint != null)
		{
			_analysisCoroutine = StartCoroutine(AnalyzeVehicle());
		}
	}

	private void OnDisable()
	{
		if (_analysisCoroutine != null)
		{
			StopCoroutine(_analysisCoroutine);
		}

		CenterOfMassIndicator.SetActive(false);
	}

	private IEnumerator AnalyzeVehicle()
	{
		_result = new VehicleAnalysisResult
		{
			Mass = 0f,
			CenterOfMass = Vector2.zero,
			MomentOfInertia = 0f
		};
		var momentOfInertiaData = new LinkedList<Tuple<Vector2, float, float>>();

		Debug.Log($"Stopwatch frequency {Stopwatch.Frequency}");
		long timestamp = Stopwatch.GetTimestamp();
		long timeThreshold = Stopwatch.Frequency / 100; // 10ms

		int blockCount = Designer.Blueprint.Blocks.Count;

		for (int progress = 0; progress < blockCount; progress++)
		{
			VehicleBlueprint.BlockInstance block = Designer.Blueprint.Blocks[progress];
			GameObject blockObject = Builder.GetBlockObject(block);

			Vector2 rootLocation = new Vector2(block.X, block.Y);

			BlockInfo info = blockObject.GetComponent<BlockInfo>();
			Vector2 blockCenter = rootLocation + RotationUtils.RotatePoint(info.CenterOfMass, block.Rotation);
			_result.Mass += info.Mass;
			_result.CenterOfMass += info.Mass * blockCenter;
			momentOfInertiaData.AddLast(new Tuple<Vector2, float, float>(blockCenter, info.Mass, info.MomentOfInertia));

			long time = Stopwatch.GetTimestamp();
			if (time - timestamp > timeThreshold)
			{
				Output.text = $"Analyzing physics {progress}/{blockCount} ({progress * 100f / blockCount:F0}%)";

				yield return null;
				timestamp = time;
			}
		}

		if (_result.Mass > Mathf.Epsilon)
		{
			_result.CenterOfMass /= _result.Mass;
		}

		foreach (Tuple<Vector2, float, float> blockData in momentOfInertiaData)
		{
			(Vector2 position, float mass, float blockMoment) = blockData;
			_result.MomentOfInertia += blockMoment + mass * (position - _result.CenterOfMass).sqrMagnitude;
		}

		Output.text = string.Join(
			"\n",
			$"Block count {blockCount}",
			$"Total mass {_result.Mass * PhysicsConstants.KG_PER_UNIT_MASS:0.#} kg",
			$"<color=\"#{ColorUtility.ToHtmlStringRGB(CenterOfMassIndicator.GetComponent<SpriteRenderer>().color)}\">• Center of mass</color>"
		);
		CenterOfMassIndicator.transform.localPosition = _result.CenterOfMass;
		CenterOfMassIndicator.SetActive(true);
	}
}
}