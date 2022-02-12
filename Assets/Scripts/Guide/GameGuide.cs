using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Blocks.Propulsion;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.Designer;
using Syy1125.OberthEffect.Designer.Config;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Guide
{
public enum GuideSelection
{
	None,
	DesignerBasic,
	VehicleBasic
}

public class GameGuide : MonoBehaviour
{
	[Header("Common")]
	public RectTransform HighlightFrame;
	public Text GuideTitle;
	public Text GuideText;
	public Button SkipStepButton;
	public float FadeTime = 0.2f;
	public float HighlightZoomTime = 0.5f;
	public float HighlightScale = 5f;
	public float SkipDelay = 10f;

	[Header("Designer")]
	public VehicleDesigner Designer;
	public VehicleBuilder Builder;

	public RectTransform Toolbar;
	public RectTransform BlockCategories;
	public RectTransform AnalyzerButton;

	public ToolWindows Tools;
	public DesignerConfig Config;
	public GameObject DesignerMenu;
	public GameObject HelpScreen;

	private RectTransform _canvasTransform;
	private CanvasGroup _canvasGroup;
	private Coroutine _zoomHighlight;
	private bool _skip;

	public static GuideSelection ActiveGuide = GuideSelection.None;
	public static GameGuide Instance { get; private set; }

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else if (Instance != this)
		{
			Destroy(this);
			return;
		}

		_canvasTransform = GetComponent<RectTransform>();
		_canvasGroup = GetComponent<CanvasGroup>();
	}

	private void OnEnable()
	{
		SkipStepButton.onClick.AddListener(SkipStep);
	}

	private void Start()
	{
		var guide = ActiveGuide;
		ActiveGuide = GuideSelection.None;
		switch (guide)
		{
			case GuideSelection.None:
				gameObject.SetActive(false);
				break;
			case GuideSelection.DesignerBasic:
				StartCoroutine(PlayDesignerGuide());
				break;
			case GuideSelection.VehicleBasic:
				StartCoroutine(PlayVehicleGuide());
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	private void OnDisable()
	{
		SkipStepButton.onClick.RemoveListener(SkipStep);
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	#region Guide Utility

	private void SkipStep()
	{
		_skip = true;
	}

	private void Highlight(RectTransform target)
	{
		if (target == null)
		{
			HighlightFrame.gameObject.SetActive(false);
		}
		else
		{
			HighlightFrame.gameObject.SetActive(true);
			Vector3[] corners = new Vector3[4];
			target.GetWorldCorners(corners);
			// Reference: https://docs.unity3d.com/ScriptReference/RectTransform.GetWorldCorners.html
			// Corners are in clockwise order starting from bottom left
			HighlightFrame.offsetMin = _canvasTransform.InverseTransformPoint(corners[0]) - new Vector3(5f, 5f, 0);
			HighlightFrame.offsetMax = _canvasTransform.InverseTransformPoint(corners[2]) + new Vector3(5f, 5f, 0);

			if (_zoomHighlight != null)
			{
				StopCoroutine(_zoomHighlight);
			}

			_zoomHighlight = StartCoroutine(ZoomHighlight());
		}
	}

	private IEnumerator ZoomHighlight()
	{
		float startTime = Time.time;
		float endTime = startTime + HighlightZoomTime;

		while (Time.time < endTime)
		{
			float scale = Mathf.Lerp(HighlightScale, 1f, Mathf.InverseLerp(startTime, endTime, Time.time));
			HighlightFrame.localScale = Vector3.one * scale;

			yield return null;
		}

		HighlightFrame.localScale = Vector3.one;
	}

	private IEnumerator FadeGuideBox(float endAlpha)
	{
		float startAlpha = _canvasGroup.alpha;
		float startTime = Time.unscaledTime;
		float endTime = startTime + FadeTime;
		do
		{
			yield return null;
			float t = Mathf.InverseLerp(startTime, endTime, Time.unscaledTime);
			_canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
		}
		while (Time.unscaledTime < endTime);
	}

	private IEnumerator Step(string title, string text, Func<bool> condition, string skipText = null)
	{
		skipText ??= condition == null ? "Continue" : "Skip";
		SkipStepButton.GetComponentInChildren<Text>().text = skipText;

		SkipStepButton.interactable = condition == null;

		GuideTitle.text = title;
		GuideText.text = text;
		yield return StartCoroutine(FadeGuideBox(1f));

		if (condition == null)
		{
			yield return new WaitUntil(() => _skip);
		}
		else
		{
			var delayedEnableSkip = StartCoroutine(DelayedEnableSkip(skipText));
			yield return new WaitUntil(() => _skip || condition());
			StopCoroutine(delayedEnableSkip);
			yield return new WaitForSeconds(0.5f);
		}

		_skip = false;
		SkipStepButton.interactable = false;
		yield return StartCoroutine(FadeGuideBox(0f));
	}

	private IEnumerator DelayedEnableSkip(string skipText)
	{
		float startTime = Time.time;
		while (Time.time - startTime < SkipDelay)
		{
			float remainingTime = startTime + SkipDelay - Time.time;
			SkipStepButton.GetComponentInChildren<Text>().text = $"{skipText} ({Mathf.CeilToInt(remainingTime)})";
			yield return null;
		}

		SkipStepButton.interactable = true;
		SkipStepButton.GetComponentInChildren<Text>().text = skipText;
	}

	private void EndGuide()
	{
		gameObject.SetActive(false);
	}

	private IEnumerable<T> GetAllBlockComponents<T>()
	{
		return Designer.Blueprint.Blocks
			.Select(instance => Builder.GetBlockObject(instance))
			.SelectMany(blockObject => blockObject.GetComponents<T>());
	}

	#endregion

	#region Designer Guide

	private IEnumerator PlayDesignerGuide()
	{
		Highlight(null);
		yield return StartCoroutine(
			Step(
				"Introduction to vehicle designer",
				string.Join(
					"\n",
					"Welcome to the vehicle designer! Here you will create your very own designs to your specifications.",
					"This short guide aims to get you familiar with the basic functionalities of the vehicle designer."
				),
				null
			)
		);

		Vector2 startPosition = Designer.transform.localPosition;
		yield return StartCoroutine(
			Step(
				"Designer controls",
				"Use WASD to move around the designer. Hold shift to pan faster. You can also right-click and drag.",
				() => ((Vector2) Designer.transform.localPosition - startPosition).sqrMagnitude > 4f
			)
		);

		yield return StartCoroutine(
			Step(
				"Designer controls",
				"Use scroll wheel to zoom in and out.",
				() => Mathf.Abs(Mathf.Log(Designer.transform.localScale.x)) > 0.5f
			)
		);

		yield return StartCoroutine(
			Step(
				"Placing blocks (1/2)",
				string.Join(
					"\n",
					"To add a block to your design, first click on it from the block palette on the right to select it, then use left click to place blocks. Use R and Shift+R for rotation.",
					"You can click and drag with the cursor to quickly place multiple blocks.",
					"Make something with 10 blocks total to complete this step."
				),
				() => Designer.Blueprint.Blocks.Count >= 10
			)
		);

		yield return StartCoroutine(
			Step(
				"Placing blocks (2/2)",
				"Use Q to deselect blocks and go back to cursor mode. E selects eraser mode for removing blocks. M toggles mirror mode.\nNote that you can never remove the control core, as a vehicle requires it to function.",
				null
			)
		);

		Highlight(null);
		yield return StartCoroutine(
			Step(
				"Saving and loading",
				"Press esc to bring up the vehicle menu, where you can save your design or load previous ones.",
				() => DesignerMenu.activeSelf
			)
		);

		yield return StartCoroutine(
			Step(
				"Help screen",
				"Press F1 to bring up the help screen, where you can review control schemes.\nThere's also some explanation of game mechanics which you may find useful.",
				() => HelpScreen.activeSelf
			)
		);

		yield return StartCoroutine(
			Step(
				"Tooltips",
				string.Join(
					"\n",
					"Oberth Effect comes with an extensive tooltip system to provide you with information.",
					"If you ever want to know what a block does, hover over it and see what the tooltip says!"
				),
				null
			)
		);

		Highlight(BlockCategories);
		yield return StartCoroutine(
			Step(
				"Block categories",
				"Blocks are grouped into categories. If you are looking for a specific block, filtering by block categories might help.",
				null
			)
		);

		Highlight(Toolbar);
		yield return StartCoroutine(
			Step(
				"Designer toolbox",
				string.Join(
					"\n",
					"So far, you have been working with the block palette. There are two other tool screens: the configuration tool and the analysis tool.",
					"The config tool lets you adjust your vehicle's behaviour and aesthetics settings, and the analysis tool is used to review expected performance of your vehicle.",
					"We will cover those in a future guide, when you're more familiar with other aspects of the game."
				),
				null
			)
		);

		yield return StartCoroutine(
			Step(
				"Conclusion",
				string.Join(
					"\n",
					"That concludes the basic features of the vehicle designer.",
					"When you are ready, you can go back to the main menu and play the Vehicle Essentials guide.",
					"Alternatively, you're welcome to experiment with the designer and see what you can come up with."
				),
				null, "Done"
			)
		);


		EndGuide();
	}

	#endregion

	#region Ship Guide

	private IEnumerator PlayVehicleGuide()
	{
		Highlight(null);
		yield return StartCoroutine(
			Step(
				"Vehicle Essentials",
				"Now that you're familiar with the basic designer controls, let's look at what a functioning vehicle needs.",
				null
			)
		);

		yield return StartCoroutine(
			Step(
				"Resources",
				string.Join(
					"\n",
					"Most systems on the vehicle requires resources to function. The vehicle's control core comes with a bit of storage and basic energy generation, but that won't be enough for larger vehicles.",
					"Bring energy generation up to over 200 per second to complete this step."
				),
				() => GetAllBlockComponents<IResourceGenerator>()
					      .Select(generator => generator.GetMaxGenerationRate())
					      .Where(rate => rate != null && rate.ContainsKey("OberthEffect/Energy"))
					      .Sum(rate => rate["OberthEffect/Energy"])
				      >= 200
			)
		);

		yield return StartCoroutine(
			Step(
				"Propulsion (1/3)",
				string.Join(
					"\n",
					"Vehicles need propulsion components to move and maneuver. The more thrusters and engines you equip, the faster you can accelerate or decelerate.",
					"When applicable, propulsion components will also be used to rotate the vehicle. So if you want your vehicle to rotate quickly, place thrusters far away from the center of mass!"
				),
				null
			)
		);

		yield return StartCoroutine(
			Step(
				"Propulsion (2/3)", string.Join(
					"\n",
					"Engines are workhorses with powerful thrust in a single direction. But they generally require time to ramp up thrust.",
					"Thrusters, on the other hand, are designed to be versatile. They are weaker and less efficient, but have fast response time and often can provide thrust in more than one direction."
				), null
			)
		);

		yield return StartCoroutine(
			Step(
				"Propulsion (3/3)",
				string.Join(
					"\n",
					"A mix of engines and thrusters is recommended to bring out the best of both worlds.",
					"To complete this step, have the vehicle able to generate thrust in all 4 cardinal directions."
				),
				() =>
				{
					float up = 0, down = 0, left = 0, right = 0;

					foreach (VehicleBlueprint.BlockInstance instance in Designer.Blueprint.Blocks)
					{
						GameObject blockObject = Builder.GetBlockObject(instance);
						foreach (IPropulsionBlock propulsion in blockObject.GetComponents<IPropulsionBlock>())
						{
							up += propulsion.GetMaxPropulsionForce(
								CardinalDirectionUtils.InverseRotate(CardinalDirection.Up, instance.Rotation)
							);
							down += propulsion.GetMaxPropulsionForce(
								CardinalDirectionUtils.InverseRotate(CardinalDirection.Down, instance.Rotation)
							);
							left += propulsion.GetMaxPropulsionForce(
								CardinalDirectionUtils.InverseRotate(CardinalDirection.Left, instance.Rotation)
							);
							right += propulsion.GetMaxPropulsionForce(
								CardinalDirectionUtils.InverseRotate(CardinalDirection.Right, instance.Rotation)
							);
						}
					}

					return !Mathf.Approximately(up, 0)
					       && !Mathf.Approximately(down, 0)
					       && !Mathf.Approximately(left, 0)
					       && !Mathf.Approximately(right, 0);
				}
			)
		);

		yield return StartCoroutine(
			Step(
				"Weapons",
				string.Join(
					"\n",
					"Vehicle-mounted weapons lets you shoot back when encountering hostiles. Never go into combat without them.",
					"A detailed description of damage mechanics can be found in the help (F1) screen."
				),
				null
			)
		);

		Highlight(AnalyzerButton);
		yield return StartCoroutine(
			Step(
				"Vehicle analyzer",
				string.Join(
					"\n",
					"The vehicle analyzer lets you see the expected performance of your vehicle design, like its resource statistics or maneuverability.",
					"Be sure to check the analyzer every now and then to make sure the vehicle's performance is within expectations."
				),
				() => Tools.SelectedIndex == 2
			)
		);

		Highlight(null);
		yield return StartCoroutine(
			Step(
				"Conclusion",
				"That concludes the vehicle essentials guide. Now go forth and create!",
				null, "Done"
			)
		);

		EndGuide();
	}

	#endregion
}
}