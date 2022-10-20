using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Blocks.Propulsion;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.CombatSystem;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Foundation.Utils;
using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Spec;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.InputSystem;
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
	public Dictionary<CardinalDirection, float> MaxPropulsion;
	public Dictionary<CardinalDirection, float> CurrentPropulsion;
	public float MaxTorqueCcw;
	public float CurrentTorqueCcw;
	public float MaxTorqueCw;
	public float CurrentTorqueCw;

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

	private VehicleBuilder Builder => Designer.Builder;
	private BlockIndicators Indicators => Designer.Indicators;

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
			MaxPropulsion = new Dictionary<CardinalDirection, float>
			{
				{ CardinalDirection.Up, 0f },
				{ CardinalDirection.Down, 0f },
				{ CardinalDirection.Left, 0f },
				{ CardinalDirection.Right, 0f }
			},
			CurrentPropulsion = new Dictionary<CardinalDirection, float>
			{
				{ CardinalDirection.Up, 0f },
				{ CardinalDirection.Down, 0f },
				{ CardinalDirection.Left, 0f },
				{ CardinalDirection.Right, 0f }
			},
			MaxTorqueCcw = 0f,
			CurrentTorqueCcw = 0f,
			MaxTorqueCw = 0f,
			CurrentTorqueCw = 0f,
			MaxResourceStorage = new(),
			MaxResourceGeneration = new(),
			MaxResourceUse = new(),
			MaxCategoryResourceUse = new(),
			MaxFirepower = new()
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

			if (behaviour is IResourceStorage storage)
			{
				DictionaryUtils.AddDictionary(storage.GetCapacity(), _result.MaxResourceStorage);
			}

			if (behaviour is IPropulsionBlock propulsion)
			{
				foreach (
					var direction in new[]
					{
						CardinalDirection.Up,
						CardinalDirection.Down,
						CardinalDirection.Left,
						CardinalDirection.Right
					}
				)
				{
					float maxForce = propulsion.GetMaxPropulsionForce(
						CardinalDirectionUtils.InverseRotate(direction, blockInstance.Rotation)
					);

					_result.MaxPropulsion[direction] += maxForce;

					if (propulsion.RespondToTranslation)
					{
						_result.CurrentPropulsion[direction] += maxForce;
					}
				}
			}

			if (behaviour is IWeaponBlock weaponSystem)
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
				float torqueCcw = propulsion.GetMaxFreeTorqueCcw();
				float torqueCw = propulsion.GetMaxFreeTorqueCw();

				Vector2 forceOrigin = blockInstance.Position
				                      + TransformUtils.RotatePoint(
					                      propulsion.GetPropulsionForceOrigin(), blockInstance.Rotation
				                      );

				if (forceOrigin.x > Mathf.Epsilon)
				{
					torqueCcw +=
						forceOrigin.x
						* propulsion.GetMaxPropulsionForce(
							CardinalDirectionUtils.InverseRotate(CardinalDirection.Up, blockInstance.Rotation)
						);
					torqueCw +=
						forceOrigin.x
						* propulsion.GetMaxPropulsionForce(
							CardinalDirectionUtils.InverseRotate(CardinalDirection.Down, blockInstance.Rotation)
						);
				}
				else if (forceOrigin.x < -Mathf.Epsilon)
				{
					torqueCcw -=
						forceOrigin.x
						* propulsion.GetMaxPropulsionForce(
							CardinalDirectionUtils.InverseRotate(CardinalDirection.Down, blockInstance.Rotation)
						);
					torqueCw -=
						forceOrigin.x
						* propulsion.GetMaxPropulsionForce(
							CardinalDirectionUtils.InverseRotate(CardinalDirection.Up, blockInstance.Rotation)
						);
				}

				if (forceOrigin.y > Mathf.Epsilon)
				{
					torqueCcw +=
						forceOrigin.y
						* propulsion.GetMaxPropulsionForce(
							CardinalDirectionUtils.InverseRotate(CardinalDirection.Left, blockInstance.Rotation)
						);
					torqueCw +=
						forceOrigin.y
						* propulsion.GetMaxPropulsionForce(
							CardinalDirectionUtils.InverseRotate(CardinalDirection.Right, blockInstance.Rotation)
						);
				}
				else if (forceOrigin.y < -Mathf.Epsilon)
				{
					torqueCcw -=
						forceOrigin.y
						* propulsion.GetMaxPropulsionForce(
							CardinalDirectionUtils.InverseRotate(CardinalDirection.Right, blockInstance.Rotation)
						);
					torqueCw -=
						forceOrigin.y
						* propulsion.GetMaxPropulsionForce(
							CardinalDirectionUtils.InverseRotate(CardinalDirection.Left, blockInstance.Rotation)
						);
				}

				_result.MaxTorqueCcw += torqueCcw;
				_result.MaxTorqueCw += torqueCw;

				if (propulsion.RespondToRotation)
				{
					_result.CurrentTorqueCcw += torqueCcw;
					_result.CurrentTorqueCw += torqueCw;
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
			$"  Total mass {PhysicsUnitUtils.FormatMass(_result.Mass)}",
			$"  Moment of inertia {PhysicsUnitUtils.FormatMomentOfInertia(_result.MomentOfInertia)}",
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
		float warnThresholdScale = 1 / Mathf.Sqrt(Mathf.Max(_result.Mass / 100, 1));

		void DisplayPropulsionAcceleration(
			Text output, CardinalDirection direction, float redThreshold, float yellowThreshold
		)
		{
			float current = _result.CurrentPropulsion[direction] / _result.Mass;
			float max = _result.MaxPropulsion[direction] / _result.Mass;
			output.text =
				$"{PhysicsUnitUtils.FormatAccelerationNumeric(current)}/{PhysicsUnitUtils.FormatAccelerationNumeric(max)}";

			if (current < redThreshold * warnThresholdScale)
			{
				output.color = Color.red;
			}
			else if (current < yellowThreshold * warnThresholdScale)
			{
				output.color = Color.yellow;
			}
			else
			{
				output.color = Color.white;
			}
		}

		void DisplayPropulsionAngularAcceleration(
			Text output, float current, float max, float redThreshold, float yellowThreshold
		)
		{
			current = current / _result.MomentOfInertia * Mathf.Rad2Deg;
			max = max / _result.MomentOfInertia * Mathf.Rad2Deg;
			output.text = $"{current:#,0.#}/{max:#,0.#}";

			if (current < redThreshold * warnThresholdScale)
			{
				output.color = Color.red;
			}
			else if (current < yellowThreshold * warnThresholdScale)
			{
				output.color = Color.yellow;
			}
			else
			{
				output.color = Color.white;
			}
		}

		void DisplayPropulsionForce(
			Text output, CardinalDirection direction, float redThreshold, float yellowThreshold
		)
		{
			float current = _result.CurrentPropulsion[direction];
			float max = _result.MaxPropulsion[direction];
			output.text = $"{PhysicsUnitUtils.FormatForceNumeric(current)}/{PhysicsUnitUtils.FormatForceNumeric(max)}";

			if (current < redThreshold * _result.Mass * warnThresholdScale)
			{
				output.color = Color.red;
			}
			else if (current < yellowThreshold * _result.Mass * warnThresholdScale)
			{
				output.color = Color.yellow;
			}
			else
			{
				output.color = Color.white;
			}
		}

		void DisplayPropulsionTorque(
			Text output, float current, float max, float redThreshold, float yellowThreshold
		)
		{
			output.text =
				$"{PhysicsUnitUtils.FormatTorqueNumeric(current)}/{PhysicsUnitUtils.FormatTorqueNumeric(max)}";

			float angularAcceleration = current / _result.MomentOfInertia * Mathf.Rad2Deg;

			if (angularAcceleration < redThreshold * warnThresholdScale)
			{
				output.color = Color.red;
			}
			else if (angularAcceleration < yellowThreshold * warnThresholdScale)
			{
				output.color = Color.yellow;
			}
			else
			{
				output.color = Color.white;
			}
		}

		if (_accelerationMode)
		{
			TranslationLabel.text = $"Acceleration ({PhysicsUnitUtils.GetAccelerationUnits()}, current/max)";
			DisplayPropulsionAcceleration(TranslationUpOutput, CardinalDirection.Up, 2f, 5f);
			DisplayPropulsionAcceleration(TranslationDownOutput, CardinalDirection.Down, 1f, 2f);
			DisplayPropulsionAcceleration(TranslationLeftOutput, CardinalDirection.Left, 1f, 2f);
			DisplayPropulsionAcceleration(TranslationRightOutput, CardinalDirection.Right, 1f, 2f);

			RotationLabel.text = "Angular acceleration\n(deg/s², current/max)";
			DisplayPropulsionAngularAcceleration(
				RotationCcwOutput, _result.CurrentTorqueCcw, _result.MaxTorqueCcw, 30f, 60f
			);
			DisplayPropulsionAngularAcceleration(
				RotationCwOutput, _result.CurrentTorqueCw, _result.MaxTorqueCw, 30f, 60f
			);

			ForceModeButton.GetComponentInChildren<Text>().color = Color.gray;
			ForceModeButton.GetComponentInChildren<Image>().enabled = false;
			AccelerationModeButton.GetComponentInChildren<Text>().color = Color.cyan;
			AccelerationModeButton.GetComponentInChildren<Image>().enabled = true;
		}
		else
		{
			TranslationLabel.text = $"Force ({PhysicsUnitUtils.GetForceUnits()}, current/max)";
			DisplayPropulsionForce(TranslationUpOutput, CardinalDirection.Up, 2f, 5f);
			DisplayPropulsionForce(TranslationDownOutput, CardinalDirection.Down, 1f, 2f);
			DisplayPropulsionForce(TranslationLeftOutput, CardinalDirection.Left, 1f, 2f);
			DisplayPropulsionForce(TranslationRightOutput, CardinalDirection.Right, 1f, 2f);

			RotationLabel.text = $"Torque ({PhysicsUnitUtils.GetTorqueUnits()}, current/max)";
			DisplayPropulsionTorque(RotationCcwOutput, _result.CurrentTorqueCcw, _result.MaxTorqueCcw, 30f, 60f);
			DisplayPropulsionTorque(RotationCwOutput, _result.CurrentTorqueCw, _result.MaxTorqueCw, 30f, 60f);

			ForceModeButton.GetComponentInChildren<Text>().color = Color.yellow;
			ForceModeButton.GetComponentInChildren<Image>().enabled = true;
			AccelerationModeButton.GetComponentInChildren<Text>().color = Color.gray;
			AccelerationModeButton.GetComponentInChildren<Image>().enabled = false;
		}

		PropulsionOutput.SetActive(true);
	}

	private void DisplayFirepowerResults()
	{
		var aggregateFirepower = FirepowerUtils.AggregateFirepower(_result.MaxFirepower);
		float maxDps = FirepowerUtils.GetTotalDamage(aggregateFirepower);
		float armorPierce = FirepowerUtils.GetMeanArmorPierce(aggregateFirepower);

		StringBuilder output = new();
		output
			.AppendLine("<b>Firepower</b>")
			.AppendLine($"  Maximum DPS {maxDps:#,0.#} (mean AP = {armorPierce:0.##})");

		foreach (FirepowerEntry entry in aggregateFirepower)
		{
			if (Mathf.Approximately(entry.DamagePerSecond, 0f)) continue;

			DamageTypeSpec damageTypeSpec = DamageTypeDatabase.Instance.GetSpec(entry.DamageTypeId);
			output.AppendLine(
				$"    {damageTypeSpec.WrapColorTag(damageTypeSpec.DisplayName)} DPS {entry.DamagePerSecond:#,0.#} (mean AP = {entry.ArmorPierce:0.##})"
			);
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
			SelectionIndicator.GetComponent<SpriteRenderer>().size = new(blockBounds.size.x, blockBounds.size.y);
			SelectionIndicator.SetActive(true);

			List<Vector2Int> attachedBlocks = new();
			List<Vector2Int> closedAttachPoints = new();
			List<Vector2Int> openAttachPoints = new();

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