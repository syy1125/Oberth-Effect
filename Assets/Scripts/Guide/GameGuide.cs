using System;
using System.Collections;
using Syy1125.OberthEffect.Designer;
using Syy1125.OberthEffect.Designer.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Guide
{
public enum GuideSelection
{
	None,
	Designer
}

public class GameGuide : MonoBehaviour
{
	[Header("Common")]
	public RectTransform HighlightFrame;
	public Text GuideTitle;
	public Text GuideText;
	public Button SkipStepButton;
	public float FadeTime = 0.2f;

	[Header("Designer")]
	public VehicleDesigner Designer;
	public ToolWindows Tools;
	public RectTransform ConfigButton;
	public DesignerConfig Config;
	public RectTransform AnalyzerButton;
	public GameObject DesignerMenu;
	public GameObject HelpScreen;

	private RectTransform _canvasTransform;
	private CanvasGroup _canvasGroup;
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
			case GuideSelection.Designer:
				PlayDesignerGuide();
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
		}
	}

	private IEnumerator Fade(float endAlpha)
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
		SkipStepButton.GetComponentInChildren<Text>().text = skipText ?? (condition == null ? "Continue" : "Skip");
		SkipStepButton.interactable = false;

		GuideTitle.text = title;
		GuideText.text = text;
		yield return StartCoroutine(Fade(1f));
		SkipStepButton.interactable = true;

		yield return new WaitUntil(() => _skip || condition != null && condition());
		_skip = false;

		SkipStepButton.interactable = false;
		yield return StartCoroutine(Fade(0f));
	}

	private void EndGuide()
	{
		gameObject.SetActive(false);
	}

	#region Designer Guide

	public void PlayDesignerGuide()
	{
		StartCoroutine(DoPlayDesignerGuide());
	}

	private IEnumerator DoPlayDesignerGuide()
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
					"To add a block to your design, first select it from the block palette on the right, then use left click to place blocks.",
					"Make something with 10 blocks total to complete this step."
				),
				() => Designer.Blueprint.Blocks.Count >= 10
			)
		);

		yield return StartCoroutine(
			Step(
				"Placing blocks (2/2)",
				"Use Q to deselect blocks and go back to cursor mode. Use E to select eraser mode for removing blocks.\nNote that you can never remove the control core, as a vehicle requires it to function.",
				null
			)
		);

		Highlight(ConfigButton);
		yield return StartCoroutine(
			Step(
				"Vehicle configuration",
				"The right side of designer has several tool panels. The configuration tool lets you adjust your vehicle's behaviour and aesthetics settings.",
				() => Tools.SelectedIndex == 1
			)
		);

		Highlight(null);
		yield return StartCoroutine(
			Step(
				"Vehicle configuration",
				"Here the configuration tool is displaying vehicle configuration.",
				null
			)
		);

		yield return StartCoroutine(
			Step(
				"Block configuration",
				"Some individual blocks, especially functional ones, have configuration properties too. Click on a block to view its configuration.",
				() => Config.HasSelectedBlock
			)
		);

		yield return StartCoroutine(
			Step(
				"Block configuration",
				"Press Q or click on empty space to go back to vehicle configuration.",
				() => !Config.HasSelectedBlock
			)
		);

		Highlight(AnalyzerButton);
		yield return StartCoroutine(
			Step(
				"Vehicle analysis",
				"The vehicle analysis tool can tell you important information about your vehicle design, like its projected acceleration or combat capabilities.",
				() => Tools.SelectedIndex == 2
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
				"Conclusion",
				"That concludes the core features of the vehicle designer. Now go forth and create!",
				null, "Done"
			)
		);

		EndGuide();
	}

	#endregion
}
}