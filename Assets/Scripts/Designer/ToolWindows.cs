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

	private int _selectedIndex;

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
		_selectedIndex = 0;
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
			Windows[i].Window.SetActive(i == _selectedIndex);
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
		Windows[_selectedIndex].Window.SetActive(false);
		_selectedIndex = index;
		Windows[_selectedIndex].Window.SetActive(true);
	}

	private void Update()
	{
		var target = Windows[_selectedIndex].Button.GetComponent<RectTransform>();
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