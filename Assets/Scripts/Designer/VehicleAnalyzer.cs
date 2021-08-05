using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Blocks.Propulsion;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.Blocks.Weapons;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Database;
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
	public float PropulsionCcw;
	public float PropulsionCw;

	// Resource
	public Dictionary<string, float> MaxResourceStorage;
	public Dictionary<string, float> MaxResourceGeneration;
	public Dictionary<string, float> MaxResourceConsumption;
	public Dictionary<string, float> MaxPropulsionResourceUse;
	public Dictionary<string, float> MaxWeaponResourceUse;

	// Firepower
	public Dictionary<DamageType, float> MaxFirepower;
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
	public Text ResourceOutput;
	[Space]
	public GameObject PropulsionOutput;
	public Button ForceModeButton;
	public Button AccelerationModeButton;
	public Text TranslationLabel;
	public Text TranslationUpOutput;
	public Text TranslationDownOutput;
	public Text TranslationLeftOutput;
	public Text TranslationRightOutput;
	public Text RotationLabel;
	public Text RotationCcwOutput;
	public Text RotationCwOutput;
	[Space]
	public Text FirepowerOutput;

	private VehicleAnalysisResult _result;
	private Coroutine _analysisCoroutine;
	private bool _accelerationMode;

	private void Awake()
	{
		int useAcc = PlayerPrefs.GetInt(PropertyKeys.ANALYSIS_USE_ACC_MODE, 0);
		_accelerationMode = useAcc == 1;
	}

	private void OnEnable()
	{
		ForceModeButton.onClick.AddListener(SetForceMode);
		AccelerationModeButton.onClick.AddListener(SetAccelerationMode);
		StartAnalysis();
	}


	private void Start()
	{
		Debug.Log($"Stopwatch frequency {Stopwatch.Frequency}");
	}

	private void OnDisable()
	{
		ForceModeButton.onClick.RemoveListener(SetForceMode);
		AccelerationModeButton.onClick.RemoveListener(SetAccelerationMode);

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
			MaxResourceStorage = new Dictionary<string, float>(),
			MaxResourceGeneration = new Dictionary<string, float>(),
			MaxResourceConsumption = new Dictionary<string, float>(),
			MaxPropulsionResourceUse = new Dictionary<string, float>(),
			MaxWeaponResourceUse = new Dictionary<string, float>(),
			MaxFirepower = new Dictionary<DamageType, float>()
		};

		long timestamp = Stopwatch.GetTimestamp();
		long timeThreshold = Stopwatch.Frequency / 100; // 10ms

		int blockCount = Designer.Blueprint.Blocks.Count;
		int totalCount = blockCount * 2; // 2 passes in an analysis

		// First pass - analyze center of mass and most components
		for (int progress = 0; progress < blockCount; progress++)
		{
			VehicleBlueprint.BlockInstance block = Designer.Blueprint.Blocks[progress];
			AnalyzeBlockFirstPass(block);

			// long time = Stopwatch.GetTimestamp();
			// if (time - timestamp > timeThreshold)
			// {
			StatusOutput.text = $"Performing analysis {progress}/{totalCount} ({progress * 100f / totalCount:F0}%)";

			yield return null;
			// timestamp = time;
			// }
		}

		if (_result.Mass > Mathf.Epsilon)
		{
			_result.CenterOfMass /= _result.Mass;
		}

		// Second pass - analyze moment of inertia and rotational motion
		for (int progress = 0; progress < blockCount; progress++)
		{
			VehicleBlueprint.BlockInstance block = Designer.Blueprint.Blocks[progress];
			AnalyzeBlockSecondPass(block);

			// long time = Stopwatch.GetTimestamp();
			// if (time - timestamp > timeThreshold)
			// {
			StatusOutput.text =
				$"Performing analysis {progress}/{totalCount} ({(blockCount + progress) * 100f / totalCount:F0}%)";

			yield return null;
			// timestamp = time;
			// }
		}

		DisplayResults();
	}

	private void AnalyzeBlockFirstPass(VehicleBlueprint.BlockInstance block)
	{
		GameObject blockObject = Builder.GetBlockObject(block);

		Vector2 rootLocation = new Vector2(block.X, block.Y);

		BlockSpec spec = BlockDatabase.Instance.GetSpecInstance(blockObject.GetComponent<BlockCore>().BlockId).Spec;
		Vector2 blockCenter = rootLocation + TransformUtils.RotatePoint(spec.Physics.CenterOfMass, block.Rotation);

		_result.Mass += spec.Physics.Mass;
		_result.CenterOfMass += spec.Physics.Mass * blockCenter;

		foreach (MonoBehaviour behaviour in blockObject.GetComponents<MonoBehaviour>())
		{
			if (behaviour is IResourceGeneratorBlock generator)
			{
				DictionaryUtils.AddDictionary(generator.GetMaxGenerationRate(), _result.MaxResourceGeneration);
			}

			if (behaviour is ResourceStorageBlock storage)
			{
				DictionaryUtils.AddDictionary(storage.GetCapacity(), _result.MaxResourceStorage);
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

				DictionaryUtils.AddDictionary(weaponSystem.GetMaxFirepower(), _result.MaxFirepower);
			}
		}
	}

	private void AnalyzeBlockSecondPass(VehicleBlueprint.BlockInstance block)
	{
		GameObject blockObject = Builder.GetBlockObject(block);

		Vector2 rootLocation = new Vector2(block.X, block.Y);
		BlockSpec spec = BlockDatabase.Instance.GetSpecInstance(blockObject.GetComponent<BlockCore>().BlockId).Spec;
		Vector2 blockCenter = rootLocation + TransformUtils.RotatePoint(spec.Physics.CenterOfMass, block.Rotation);

		_result.MomentOfInertia += spec.Physics.MomentOfInertia
		                           + spec.Physics.Mass * (blockCenter - _result.CenterOfMass).sqrMagnitude;

		foreach (MonoBehaviour behaviour in blockObject.GetComponents<MonoBehaviour>())
		{
			if (behaviour is IPropulsionBlock propulsion)
			{
				Vector2 forceOrigin =
					rootLocation + TransformUtils.RotatePoint(propulsion.GetPropulsionForceOrigin(), block.Rotation);

				if (forceOrigin.x > Mathf.Epsilon)
				{
					_result.PropulsionCcw +=
						forceOrigin.x
						* propulsion.GetMaxPropulsionForce(
							CardinalDirectionUtils.InverseRotate(CardinalDirection.Up, block.Rotation)
						);
					_result.PropulsionCw +=
						forceOrigin.x
						* propulsion.GetMaxPropulsionForce(
							CardinalDirectionUtils.InverseRotate(CardinalDirection.Down, block.Rotation)
						);
				}
				else if (forceOrigin.x < -Mathf.Epsilon)
				{
					_result.PropulsionCcw -=
						forceOrigin.x
						* propulsion.GetMaxPropulsionForce(
							CardinalDirectionUtils.InverseRotate(CardinalDirection.Down, block.Rotation)
						);
					_result.PropulsionCw -=
						forceOrigin.x
						* propulsion.GetMaxPropulsionForce(
							CardinalDirectionUtils.InverseRotate(CardinalDirection.Up, block.Rotation)
						);
				}

				if (forceOrigin.y > Mathf.Epsilon)
				{
					_result.PropulsionCcw +=
						forceOrigin.y
						* propulsion.GetMaxPropulsionForce(
							CardinalDirectionUtils.InverseRotate(CardinalDirection.Left, block.Rotation)
						);
					_result.PropulsionCw +=
						forceOrigin.y
						* propulsion.GetMaxPropulsionForce(
							CardinalDirectionUtils.InverseRotate(CardinalDirection.Right, block.Rotation)
						);
				}
				else if (forceOrigin.y < -Mathf.Epsilon)
				{
					_result.PropulsionCcw -=
						forceOrigin.y
						* propulsion.GetMaxPropulsionForce(
							CardinalDirectionUtils.InverseRotate(CardinalDirection.Right, block.Rotation)
						);
					_result.PropulsionCw -=
						forceOrigin.y
						* propulsion.GetMaxPropulsionForce(
							CardinalDirectionUtils.InverseRotate(CardinalDirection.Left, block.Rotation)
						);
				}
			}
		}
	}

	private void DisplayResults()
	{
		DisplayPhysicsResults();
		DisplayResourceResults();
		DisplayPropulsionResults();
		DisplayFirepowerResults();

		LayoutRebuilder.MarkLayoutForRebuild(OutputParent);
	}

	private void DisplayPhysicsResults()
	{
		StatusOutput.text = "Analysis Results";
		PhysicsOutput.text = string.Join(
			"\n",
			"<b>Physics</b>",
			$"  Block count {Designer.Blueprint.Blocks.Count}",
			$"  Total mass {_result.Mass * PhysicsConstants.KG_PER_UNIT_MASS:#,0.#}kg",
			$"  Moment of inertia {_result.MomentOfInertia * PhysicsConstants.KG_PER_UNIT_MASS * PhysicsConstants.METERS_PER_UNIT_LENGTH * PhysicsConstants.METERS_PER_UNIT_LENGTH:#,0.#}kgm²",
			$"  <color=\"#{ColorUtility.ToHtmlStringRGB(CenterOfMassIndicator.GetComponent<SpriteRenderer>().color)}\">• Center of mass</color>"
		);
		PhysicsOutput.gameObject.SetActive(true);
		CenterOfMassIndicator.transform.localPosition = _result.CenterOfMass;
		CenterOfMassIndicator.SetActive(true);
	}

	private void DisplayResourceResults()
	{
		ResourceOutput.text = string.Join(
			"\n",
			"<b>Resources</b>",
			"  Max storage",
			"    " + string.Join(", ", VehicleResourceDatabase.Instance.FormatResourceDict(_result.MaxResourceStorage)),
			"  Max resource generation",
			"    "
			+ string.Join(", ", VehicleResourceDatabase.Instance.FormatResourceDict(_result.MaxResourceGeneration)),
			"  Max resource consumption",
			FormatResourceUseRate("    ", "Total", _result.MaxResourceConsumption),
			FormatResourceUseRate("    ", "Propulsion", _result.MaxPropulsionResourceUse),
			FormatResourceUseRate("    ", "Weapon systems", _result.MaxWeaponResourceUse)
		);
	}

	private void DisplayPropulsionResults()
	{
		if (_accelerationMode)
		{
			TranslationLabel.text = "Theoretical maximum acceleration (m/s²)";
			TranslationUpOutput.text =
				$"{_result.PropulsionUp / _result.Mass * PhysicsConstants.METERS_PER_UNIT_LENGTH:#,0.#}";
			TranslationDownOutput.text =
				$"{_result.PropulsionDown / _result.Mass * PhysicsConstants.METERS_PER_UNIT_LENGTH:#,0.#}";
			TranslationLeftOutput.text =
				$"{_result.PropulsionLeft / _result.Mass * PhysicsConstants.METERS_PER_UNIT_LENGTH:#,0.#}";
			TranslationRightOutput.text =
				$"{_result.PropulsionRight / _result.Mass * PhysicsConstants.METERS_PER_UNIT_LENGTH:#,0.#}";

			RotationLabel.text = "Theoretical maximum angular acceleration (deg/s²)";
			RotationCcwOutput.text = $"{_result.PropulsionCcw / _result.MomentOfInertia * Mathf.Rad2Deg:#,0.#}";
			RotationCwOutput.text = $"{_result.PropulsionCw / _result.MomentOfInertia * Mathf.Rad2Deg:#,0.#}";

			ForceModeButton.GetComponentInChildren<Text>().color = Color.gray;
			ForceModeButton.GetComponentInChildren<Image>().enabled = false;
			AccelerationModeButton.GetComponentInChildren<Text>().color = Color.cyan;
			AccelerationModeButton.GetComponentInChildren<Image>().enabled = true;

			PropulsionOutput.SetActive(true);
		}
		else
		{
			TranslationLabel.text = "Theoretical maximum force (kN)";
			TranslationUpOutput.text = $"{_result.PropulsionUp * PhysicsConstants.KN_PER_UNIT_FORCE:#,0.#}";
			TranslationDownOutput.text = $"{_result.PropulsionDown * PhysicsConstants.KN_PER_UNIT_FORCE:#,0.#}";
			TranslationLeftOutput.text = $"{_result.PropulsionLeft * PhysicsConstants.KN_PER_UNIT_FORCE:#,0.#}";
			TranslationRightOutput.text = $"{_result.PropulsionRight * PhysicsConstants.KN_PER_UNIT_FORCE:#,0.#}";

			RotationLabel.text = "Theoretical maximum torque (kNm)";
			RotationCcwOutput.text =
				$"{_result.PropulsionCcw * PhysicsConstants.KN_PER_UNIT_FORCE * PhysicsConstants.METERS_PER_UNIT_LENGTH:#,0.#}";
			RotationCwOutput.text =
				$"{_result.PropulsionCw * PhysicsConstants.KN_PER_UNIT_FORCE * PhysicsConstants.METERS_PER_UNIT_LENGTH:#,0.#}";

			ForceModeButton.GetComponentInChildren<Text>().color = Color.yellow;
			ForceModeButton.GetComponentInChildren<Image>().enabled = true;
			AccelerationModeButton.GetComponentInChildren<Text>().color = Color.gray;
			AccelerationModeButton.GetComponentInChildren<Image>().enabled = false;

			PropulsionOutput.SetActive(true);
		}
	}

	private void DisplayFirepowerResults()
	{
		_result.MaxFirepower.TryGetValue(DamageType.Kinetic, out float kineticDamage);
		_result.MaxFirepower.TryGetValue(DamageType.Energy, out float energyDamage);
		_result.MaxFirepower.TryGetValue(DamageType.Explosive, out float explosiveDamage);
		FirepowerOutput.text = string.Join(
			"\n",
			"<b>Firepower</b>",
			"  Theoretical maximum DPS",
			$"    Total {_result.MaxFirepower.Values.Sum():#,0.#}",
			$"    Kinetic {kineticDamage:#,0.#}",
			$"    Energy {energyDamage:#,0.#}",
			$"    Explosive {explosiveDamage:#,0.#}"
		);
		FirepowerOutput.gameObject.SetActive(true);
	}

	private void SetForceMode()
	{
		_accelerationMode = false;
		PlayerPrefs.SetInt(PropertyKeys.ANALYSIS_USE_ACC_MODE, 0);
		DisplayPropulsionResults();
	}

	private void SetAccelerationMode()
	{
		_accelerationMode = true;
		PlayerPrefs.SetInt(PropertyKeys.ANALYSIS_USE_ACC_MODE, 1);
		DisplayPropulsionResults();
	}

	#region String Formatting

	private static string FormatResourceUseRate(string indent, string label, Dictionary<string, float> useRate)
	{
		StringBuilder builder = new StringBuilder();

		builder.Append(indent);
		builder.Append(label);

		if (useRate.Count == 0)
		{
			builder.Append(" N/A");
		}
		else
		{
			builder.AppendLine()
				.Append(indent)
				.Append("  ")
				.Append(string.Join(", ", VehicleResourceDatabase.Instance.FormatResourceDict(useRate)));
		}

		return builder.ToString();
	}

	#endregion
}
}