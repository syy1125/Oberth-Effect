using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Blocks.Propulsion;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.Blocks.Weapons;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Utils;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Syy1125.OberthEffect.Designer
{
public struct VehicleAnalysisResult
{
	// Physics
	public float Mass;
	public Vector2 CenterOfMass;
	public float MomentOfInertia;

	// Propulsion
	public float PropulsionUp;
	public float PropulsionDown;
	public float PropulsionRight;
	public float PropulsionLeft;

	// Resource
	public Dictionary<VehicleResource, float> MaxResourceStorage;
	public Dictionary<VehicleResource, float> MaxResourceGeneration;
	public Dictionary<VehicleResource, float> MaxResourceConsumption;
	public Dictionary<VehicleResource, float> MaxPropulsionResourceUse;
	public Dictionary<VehicleResource, float> MaxWeaponResourceUse;
}

public class VehicleAnalyzer : MonoBehaviour
{
	[Header("References")]
	public VehicleDesigner Designer;
	public VehicleBuilder Builder;

	[Header("Output")]
	public RectTransform OutputParent;
	[Space]
	public Text StatusOutput;
	public Text PhysicsOutput;
	public GameObject CenterOfMassIndicator;
	[Space]
	public GameObject PropulsionOutput;
	public Text PropulsionUpOutput;
	public Text PropulsionDownOutput;
	public Text PropulsionLeftOutput;
	public Text PropulsionRightOutput;
	public Text AccelerationUpOutput;
	public Text AccelerationDownOutput;
	public Text AccelerationLeftOutput;
	public Text AccelerationRightOutput;

	private VehicleAnalysisResult _result;
	private Coroutine _analysisCoroutine;

	private void OnEnable()
	{
		StartAnalysis();
	}

	private void OnDisable()
	{
		if (_analysisCoroutine != null)
		{
			StopCoroutine(_analysisCoroutine);
		}

		CenterOfMassIndicator.SetActive(false);
	}

	public void StartAnalysis()
	{
		if (!gameObject.activeSelf) return;

		if (_analysisCoroutine != null)
		{
			StopCoroutine(_analysisCoroutine);
		}

		if (Designer.Blueprint != null)
		{
			_analysisCoroutine = StartCoroutine(AnalyzeVehicle());
		}
	}

	private IEnumerator AnalyzeVehicle()
	{
		StatusOutput.gameObject.SetActive(true);
		PhysicsOutput.gameObject.SetActive(false);
		PropulsionOutput.SetActive(false);
		CenterOfMassIndicator.SetActive(false);

		_result = new VehicleAnalysisResult
		{
			Mass = 0f,
			CenterOfMass = Vector2.zero,
			MomentOfInertia = 0f,
			PropulsionUp = 0f,
			PropulsionDown = 0f,
			PropulsionLeft = 0f,
			PropulsionRight = 0f,
			MaxResourceStorage = new Dictionary<VehicleResource, float>(),
			MaxResourceGeneration = new Dictionary<VehicleResource, float>(),
			MaxResourceConsumption = new Dictionary<VehicleResource, float>(),
			MaxPropulsionResourceUse = new Dictionary<VehicleResource, float>(),
			MaxWeaponResourceUse = new Dictionary<VehicleResource, float>()
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

			foreach (MonoBehaviour behaviour in blockObject.GetComponents<MonoBehaviour>())
			{
				if (behaviour is IResourceGeneratorBlock generator)
				{
					DictionaryUtils.AddDictionary(generator.GetMaxGenerationRate(), _result.MaxResourceGeneration);
				}

				if (behaviour is ResourceStorageBlock storage)
				{
					DictionaryUtils.AddDictionary(storage.ResourceCapacityDict, _result.MaxResourceStorage);
				}

				if (behaviour is IPropulsionBlock propulsion)
				{
					DictionaryUtils.AddDictionary(propulsion.GetMaxResourceUseRate(), _result.MaxPropulsionResourceUse);
					DictionaryUtils.AddDictionary(propulsion.GetMaxResourceUseRate(), _result.MaxResourceConsumption);

					_result.PropulsionUp += propulsion.GetMaxPropulsionForce(
						CardinalDirectionUtils.InverseRotate(CardinalDirection.Up, block.Rotation)
					);
					_result.PropulsionDown += propulsion.GetMaxPropulsionForce(
						CardinalDirectionUtils.InverseRotate(CardinalDirection.Down, block.Rotation)
					);
					_result.PropulsionLeft += propulsion.GetMaxPropulsionForce(
						CardinalDirectionUtils.InverseRotate(CardinalDirection.Left, block.Rotation)
					);
					_result.PropulsionRight += propulsion.GetMaxPropulsionForce(
						CardinalDirectionUtils.InverseRotate(CardinalDirection.Right, block.Rotation)
					);
				}

				if (behaviour is IWeaponSystem weaponSystem)
				{
					DictionaryUtils.AddDictionary(weaponSystem.GetMaxResourceUseRate(), _result.MaxWeaponResourceUse);
					DictionaryUtils.AddDictionary(weaponSystem.GetMaxResourceUseRate(), _result.MaxResourceConsumption);
				}
			}

			// long time = Stopwatch.GetTimestamp();
			// if (time - timestamp > timeThreshold)
			// {
			StatusOutput.text = $"Performing analysis {progress}/{blockCount} ({progress * 100f / blockCount:F0}%)";

			yield return null;
			// timestamp = time;
			// }
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

		StatusOutput.text = "Analysis Results";
		PhysicsOutput.text = string.Join(
			"\n",
			"Physics",
			$"  Block count {blockCount}",
			$"  Total mass {_result.Mass * PhysicsConstants.KG_PER_UNIT_MASS:0.#} kg",
			$"  <color=\"#{ColorUtility.ToHtmlStringRGB(CenterOfMassIndicator.GetComponent<SpriteRenderer>().color)}\">• Center of mass</color>"
		);
		PhysicsOutput.gameObject.SetActive(true);
		CenterOfMassIndicator.transform.localPosition = _result.CenterOfMass;
		CenterOfMassIndicator.SetActive(true);

		PropulsionUpOutput.text = $"{_result.PropulsionUp * PhysicsConstants.KN_PER_UNIT_FORCE:#,0.#}";
		PropulsionDownOutput.text = $"{_result.PropulsionDown * PhysicsConstants.KN_PER_UNIT_FORCE:#,0.#}";
		PropulsionLeftOutput.text = $"{_result.PropulsionLeft * PhysicsConstants.KN_PER_UNIT_FORCE:#,0.#}";
		PropulsionRightOutput.text = $"{_result.PropulsionRight * PhysicsConstants.KN_PER_UNIT_FORCE:#,0.#}";
		AccelerationUpOutput.text =
			$"{_result.PropulsionUp / _result.Mass * PhysicsConstants.METERS_PER_UNIT_LENGTH:0.0#}";
		AccelerationDownOutput.text =
			$"{_result.PropulsionDown / _result.Mass * PhysicsConstants.METERS_PER_UNIT_LENGTH:0.0#}";
		AccelerationLeftOutput.text =
			$"{_result.PropulsionLeft / _result.Mass * PhysicsConstants.METERS_PER_UNIT_LENGTH:0.0#}";
		AccelerationRightOutput.text =
			$"{_result.PropulsionRight / _result.Mass * PhysicsConstants.METERS_PER_UNIT_LENGTH:0.0#}";
		PropulsionOutput.SetActive(true);

		LayoutRebuilder.MarkLayoutForRebuild(OutputParent);
	}
}
}