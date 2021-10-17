using System;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Designer
{
public class ToolWindows : MonoBehaviour
{
	[Serializable]
	public struct ToolWindow
	{
		public Button Button;
		public GameObject Window;
	}

	public RectTransform TabIndicator;
	public float IndicatorMoveTime;
	public ToolWindow[] Windows;

	public int SelectedIndex { get; private set; }

	private struct RectTransformVelocity
	{
		public Vector2 AnchorMinVelocity;
		public Vector2 AnchorMaxVelocity;
		public Vector2 OffsetMinVelocity;
		public Vector2 OffsetMaxVelocity;
	}

	private RectTransformVelocity _velocity;

	private void Awake()
	{
		SelectedIndex = 0;
		_velocity = new RectTransformVelocity();
	}

	private void OnEnable()
	{
		for (var index = 0; index < Windows.Length; index++)
		{
			int i = index;
			Windows[index].Button.onClick.AddListener(() => SelectIndex(i));
		}
	}

	private void Start()
	{
		for (var i = 0; i < Windows.Length; i++)
		{
			Windows[i].Window.SetActive(i == SelectedIndex);
		}
	}

	private void OnDisable()
	{
		foreach (ToolWindow window in Windows)
		{
			window.Button.onClick.RemoveAllListeners();
		}
	}

	private void SelectIndex(int index)
	{
		Windows[SelectedIndex].Window.SetActive(false);
		SelectedIndex = index;
		Windows[SelectedIndex].Window.SetActive(true);
	}

	private void Update()
	{
		var target = Windows[SelectedIndex].Button.GetComponent<RectTransform>();
		TabIndicator.anchorMin = Vector2.SmoothDamp(
			TabIndicator.anchorMin, target.anchorMin, ref _velocity.AnchorMinVelocity, IndicatorMoveTime
		);
		TabIndicator.anchorMax = Vector2.SmoothDamp(
			TabIndicator.anchorMax, target.anchorMax, ref _velocity.AnchorMaxVelocity, IndicatorMoveTime
		);
		TabIndicator.offsetMin = Vector2.SmoothDamp(
			TabIndicator.offsetMin, target.offsetMin, ref _velocity.OffsetMinVelocity, IndicatorMoveTime
		);
		TabIndicator.offsetMax = Vector2.SmoothDamp(
			TabIndicator.offsetMax, target.offsetMax, ref _velocity.OffsetMaxVelocity, IndicatorMoveTime
		);
	}
}
}