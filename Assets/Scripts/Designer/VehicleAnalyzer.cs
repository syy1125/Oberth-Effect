using System;
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
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Utils;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
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
	public Dictionary<string, float> MaxResourceUse;
	public Dictionary<string, Dictionary<string, float>> MaxCategoryResourceUse;

	// Firepower
	public List<FirepowerEntry> MaxFirepower;
}

public class VehicleAnalyzer : MonoBehaviour
{
	#region Unity Fields

	[Header("References")]
	public VehicleDesigner Designer;
	public VehicleBuilder Builder;
	public BlockIndicators Indicators;
	public DesignerAreaMask AreaMask;

	[Header("Input")]
	public InputActionReference SelectBlockAction;

	[Header("Vehicle Output")]
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

	[Header("Block Analysis")]
	public GameObject SelectionIndicator;

	#endregion

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
		SelectBlockAction.action.performed += HandleClick;

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
		SelectBlockAction.action.performed -= HandleClick;

		ForceModeButton.onClick.RemoveListener(SetForceMode);
		AccelerationModeButton.onClick.RemoveListener(SetAccelerationMode);

		if (_analysisCoroutine != null)
		{
			StopCoroutine(_analysisCoroutine);
		}

		CenterOfMassIndicator.SetActive(false);
		SelectionIndicator.SetActive(false);
		Indicators.SetAttachmentPoints(null, null, null);
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
			MaxResourceUse = new Dictionary<string, float>(),
			MaxCategoryResourceUse = new Dictionary<string, Dictionary<string, float>>(),
			MaxFirepower = new List<FirepowerEntry>()
		};

		int frames = 1;

		long startTime = Stopwatch.GetTimestamp();
		long timestamp = startTime;
		long timeThreshold = Stopwatch.Frequency / 100; // 10ms

		int blockCount = Designer.Blueprint.Blocks.Count;
		int totalCount = blockCount * 2; // 2 passes in an analysis

		// First pass - analyze center of mass and most components
		for (int progress = 0; progress < blockCount; progress++)
		{
			VehicleBlueprint.BlockInstance block = Designer.Blueprint.Blocks[progress];
			AnalyzeBlockFirstPass(block);

			long time = Stopwatch.GetTimestamp();
			if (time - timestamp > timeThreshold)
			{
				StatusOutput.text = $"Performing analysis {progress}/{totalCount} ({progress * 100f / totalCount:F0}%)";

				yield return null;
				frames++;
				timestamp = time;
			}
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

			long time = Stopwatch.GetTimestamp();
			if (time - timestamp > timeThreshold)
			{
				StatusOutput.text =
					$"Performing analysis {progress}/{totalCount} ({(blockCount + progress) * 100f / totalCount:F0}%)";

				yield return null;
				frames++;
				timestamp = time;
			}
		}

		long endTime = Stopwatch.GetTimestamp();
		Debug.Log(
			$"Analysis of {blockCount} blocks done in {(float) (endTime - startTime) / Stopwatch.Frequency * 1000}ms over {frames} frames"
		);
		DisplayResults();
	}

	#region Analyze Vehicle

	private void AnalyzeBlockFirstPass(VehicleBlueprint.BlockInstance blockInstance)
	{
		GameObject blockObject = Builder.GetBlockObject(blockInstance);

		BlockSpec spec = BlockDatabase.Instance.GetBlockSpec(blockObject.GetComponent<BlockCore>().BlockId);
		Vector2 blockCenter = blockInstance.Position
		                      + TransformUtils.RotatePoint(spec.Physics.CenterOfMass, blockInstance.Rotation);

		_result.Mass += spec.Physics.Mass;
		_result.CenterOfMass += spec.Physics.Mass * blockCenter;

		foreach (MonoBehaviour behaviour in blockObject.GetComponents<MonoBehaviour>())
		{
			if (behaviour is IResourceGenerator generator)
			{
				DictionaryUtils.AddDictionary(generator.GetMaxGenerationRate(), _result.MaxResourceGeneration);
			}

			if (behaviour is IResourceConsumer consumer)
			{
				var maxResourceUse = consumer.GetMaxResourceUseRate();
				if (maxResourceUse == null) continue;

				DictionaryUtils.AddDictionary(maxResourceUse, _result.MaxResourceUse);

				if (!_result.MaxCategoryResourceUse.TryGetValue(
					spec.CategoryId, out Dictionary<string, float> categoryResourceUse
				))
				{
					categoryResourceUse = new Dictionary<string, float>();
					_result.MaxCategoryResourceUse.Add(spec.CategoryId, categoryResourceUse);
				}

				DictionaryUtils.AddDictionary(maxResourceUse, categoryResourceUse);
			}

			if (behaviour is ResourceStorage storage)
			{
				DictionaryUtils.AddDictionary(storage.GetCapacity(), _result.MaxResourceStorage);
			}

			if (behaviour is IPropulsionBlock propulsion)
			{
				_result.PropulsionUp += propulsion.GetMaxPropulsionForce(
					CardinalDirectionUtils.InverseRotate(CardinalDirection.Up, blockInstance.Rotation)
				);
				_result.PropulsionDown += propulsion.GetMaxPropulsionForce(
					CardinalDirectionUtils.InverseRotate(CardinalDirection.Down, blockInstance.Rotation)
				);
				_result.PropulsionLeft += propulsion.GetMaxPropulsionForce(
					CardinalDirectionUtils.InverseRotate(CardinalDirection.Left, blockInstance.Rotation)
				);
				_result.PropulsionRight += propulsion.GetMaxPropulsionForce(
					CardinalDirectionUtils.InverseRotate(CardinalDirection.Right, blockInstance.Rotation)
				);
			}

			if (behaviour is IWeaponSystem weaponSystem)
			{
				weaponSystem.GetMaxFirepower(_result.MaxFirepower);
			}
		}
	}

	private void AnalyzeBlockSecondPass(VehicleBlueprint.BlockInstance blockInstance)
	{
		GameObject blockObject = Builder.GetBlockObject(blockInstance);

		BlockSpec spec = BlockDatabase.Instance.GetBlockSpec(blockObject.GetComponent<BlockCore>().BlockId);
		Vector2 blockCenter = blockInstance.Position
		                      + TransformUtils.RotatePoint(spec.Physics.CenterOfMass, blockInstance.Rotation);

		_result.MomentOfInertia += spec.Physics.MomentOfInertia
		                           + spec.Physics.Mass * (blockCenter - _result.CenterOfMass).sqrMagnitude;

		foreach (MonoBehaviour behaviour in blockObject.GetComponents<MonoBehaviour>())
		{
			if (behaviour is IPropulsionBlock propulsion)
			{
				Vector2 forceOrigin = blockInstance.Position
				                      + TransformUtils.RotatePoint(
					                      propulsion.GetPropulsionForceOrigin(), blockInstance.Rotation
				                      );

				if (forceOrigin.x > Mathf.Epsilon)
				{
					_result.PropulsionCcw +=
						forceOrigin.x
						* propulsion.GetMaxPropulsionForce(
							CardinalDirectionUtils.InverseRotate(CardinalDirection.Up, blockInstance.Rotation)
						);
					_result.PropulsionCw +=
						forceOrigin.x
						* propulsion.GetMaxPropulsionForce(
							CardinalDirectionUtils.InverseRotate(CardinalDirection.Down, blockInstance.Rotation)
						);
				}
				else if (forceOrigin.x < -Mathf.Epsilon)
				{
					_result.PropulsionCcw -=
						forceOrigin.x
						* propulsion.GetMaxPropulsionForce(
							CardinalDirectionUtils.InverseRotate(CardinalDirection.Down, blockInstance.Rotation)
						);
					_result.PropulsionCw -=
						forceOrigin.x
						* propulsion.GetMaxPropulsionForce(
							CardinalDirectionUtils.InverseRotate(CardinalDirection.Up, blockInstance.Rotation)
						);
				}

				if (forceOrigin.y > Mathf.Epsilon)
				{
					_result.PropulsionCcw +=
						forceOrigin.y
						* propulsion.GetMaxPropulsionForce(
							CardinalDirectionUtils.InverseRotate(CardinalDirection.Left, blockInstance.Rotation)
						);
					_result.PropulsionCw +=
						forceOrigin.y
						* propulsion.GetMaxPropulsionForce(
							CardinalDirectionUtils.InverseRotate(CardinalDirection.Right, blockInstance.Rotation)
						);
				}
				else if (forceOrigin.y < -Mathf.Epsilon)
				{
					_result.PropulsionCcw -=
						forceOrigin.y
						* propulsion.GetMaxPropulsionForce(
							CardinalDirectionUtils.InverseRotate(CardinalDirection.Right, blockInstance.Rotation)
						);
					_result.PropulsionCw -=
						forceOrigin.y
						* propulsion.GetMaxPropulsionForce(
							CardinalDirectionUtils.InverseRotate(CardinalDirection.Left, blockInstance.Rotation)
						);
				}
			}
		}
	}

	#endregion

	#region Output Display

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
		StringBuilder output = new StringBuilder();
		output.AppendLine("<b>Resources</b>")
			.Append("  Max storage ")
			.AppendLine(
				string.Join(", ", VehicleResourceDatabase.Instance.FormatResourceDict(_result.MaxResourceStorage))
			)
			.Append("  Max resource generation ")
			.AppendLine(
				string.Join(
					", ",
					VehicleResourceDatabase.Instance
						.FormatResourceDict(_result.MaxResourceGeneration)
						.Select(entry => $"{entry}/s")
				)
			)
			.Append("  Max resource consumption ")
			.AppendLine(
				string.Join(
					", ",
					VehicleResourceDatabase.Instance.FormatResourceDict(_result.MaxResourceUse)
						.Select(entry => $"{entry}/s")
				)
			);

		var blockCategories = BlockDatabase.Instance.ListCategories()
			.Select(instance => instance.Spec)
			.Where(category => _result.MaxCategoryResourceUse.ContainsKey(category.BlockCategoryId));
		foreach (BlockCategorySpec categorySpec in blockCategories)
		{
			Dictionary<string, float> categoryResourceUse =
				_result.MaxCategoryResourceUse[categorySpec.BlockCategoryId];
			if (categoryResourceUse.Count == 0 || categoryResourceUse.Values.All(rate => Mathf.Approximately(rate, 0f)))
			{
				continue;
			}

			output.Append("    ")
				.Append(categorySpec.DisplayName)
				.Append(" ")
				.AppendLine(
					string.Join(
						", ",
						VehicleResourceDatabase.Instance.FormatResourceDict(categoryResourceUse)
							.Select(entry => $"{entry}/s")
					)
				);
		}

		ResourceOutput.text = output.ToString().TrimEnd();
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
		var aggregateFirepower = FirepowerUtils.AggregateFirepower(_result.MaxFirepower);
		float maxDps = FirepowerUtils.GetTotalDamage(aggregateFirepower);
		float armorPierce = FirepowerUtils.GetMeanArmorPierce(aggregateFirepower);

		float kineticDamage = 0f,
			kineticArmorPierce = 0f,
			energyDamage = 0f,
			energyArmorPierce = 0f,
			explosiveDamage = 0f,
			explosiveArmorPierce = 0f;

		foreach (FirepowerEntry entry in aggregateFirepower)
		{
			switch (entry.DamageType)
			{
				case DamageType.Kinetic:
					kineticDamage = entry.DamagePerSecond;
					kineticArmorPierce = entry.ArmorPierce;
					break;
				case DamageType.Energy:
					energyDamage = entry.DamagePerSecond;
					energyArmorPierce = entry.ArmorPierce;
					break;
				case DamageType.Explosive:
					explosiveDamage = entry.DamagePerSecond;
					explosiveArmorPierce = entry.ArmorPierce;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		StringBuilder output = new StringBuilder();
		output.AppendLine("<b>Firepower</b>")
			.AppendLine("  Maximum DPS")
			.AppendLine($"    Total {maxDps:#,0.#} (mean AP {armorPierce:0.##})");

		if (kineticDamage > Mathf.Epsilon)
		{
			output.AppendLine($"    Kinetic {kineticDamage:#,0.#} (mean AP {kineticArmorPierce:0.##})");
		}

		if (energyDamage > Mathf.Epsilon)
		{
			output.AppendLine($"    Energy {energyDamage:#,0.#} (mean AP {energyArmorPierce:0.##})");
		}

		if (explosiveDamage > Mathf.Epsilon)
		{
			output.AppendLine($"    Explosive {explosiveDamage:#,0.#} (mean AP {explosiveArmorPierce:0.##})");
		}

		FirepowerOutput.text = output.ToString();
		FirepowerOutput.gameObject.SetActive(true);
	}

	#endregion

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

	#region Analyze Block

	private void HandleClick(InputAction.CallbackContext context)
	{
		SetTargetBlockPosition(Designer.HoverPositionInt);
	}

	private void SetTargetBlockPosition(Vector2Int position)
	{
		if (Builder.HasBlockAt(position))
		{
			VehicleBlueprint.BlockInstance blockInstance = Builder.GetBlockInstanceAt(position);
			BlockSpec spec = BlockDatabase.Instance.GetBlockSpec(blockInstance.BlockId);

			BoundsInt blockBounds = TransformUtils.TransformBounds(
				new BlockBounds(spec.Construction.BoundsMin, spec.Construction.BoundsMax).ToBoundsInt(),
				blockInstance.Position, blockInstance.Rotation
			);
			SelectionIndicator.transform.localPosition = blockBounds.center - new Vector3(0.5f, 0.5f, 0f);
			SelectionIndicator.transform.localScale = blockBounds.size;
			SelectionIndicator.SetActive(true);

			List<Vector2Int> attachedBlocks = new List<Vector2Int>();
			List<Vector2Int> closedAttachPoints = new List<Vector2Int>();
			List<Vector2Int> openAttachPoints = new List<Vector2Int>();

			foreach (Vector2Int attachmentPoint in VehicleBlockUtils.GetAttachmentPoints(blockInstance))
			{
				if (Builder.HasBlockAt(attachmentPoint))
				{
					// The block is attached in this direction if we can form a two-way attachment
					bool attached = false;
					foreach (Vector2Int reverseAttachPoint in VehicleBlockUtils.GetAttachmentPoints(
						Builder.GetBlockInstanceAt(attachmentPoint)
					))
					{
						if (
							Builder.HasBlockAt(reverseAttachPoint)
							&& Builder.GetBlockInstanceAt(reverseAttachPoint) == blockInstance
						)
						{
							attached = true;
							break;
						}
					}

					if (attached)
					{
						attachedBlocks.Add(attachmentPoint);
					}
					else
					{
						closedAttachPoints.Add(attachmentPoint);
					}
				}
				else
				{
					openAttachPoints.Add(attachmentPoint);
				}
			}

			Indicators.SetAttachmentPoints(attachedBlocks, closedAttachPoints, openAttachPoints);
		}
		else
		{
			SelectionIndicator.SetActive(false);
			Indicators.SetAttachmentPoints(null, null, null);
		}
	}

	#endregion

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