using System;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
[RequireComponent(typeof(CanvasGroup))]
public class HudGroup : MonoBehaviour
{
	public HeadsUpDisplayMode RequiredLevel;

	private CanvasGroup _group;
	private LayoutElement _layout;

	private void Awake()
	{
		_group = GetComponent<CanvasGroup>();
		_layout = GetComponent<LayoutElement>();
	}

	private void OnEnable()
	{
		PlayerControlConfig.Instance.HudModeChanged.AddListener(UpdateVisibility);
	}

	private void Start()
	{
		UpdateVisibility();
	}

	private void OnDisable()
	{
		PlayerControlConfig.Instance.HudModeChanged.RemoveListener(UpdateVisibility);
	}

	private void UpdateVisibility()
	{
		if (PlayerControlConfig.Instance.HudMode >= RequiredLevel)
		{
			_group.alpha = 1f;
			_group.interactable = true;
			_group.blocksRaycasts = true;
			if (_layout != null) _layout.ignoreLayout = false;
		}
		else
		{
			_group.alpha = 0f;
			_group.interactable = false;
			_group.blocksRaycasts = false;
			if (_layout != null) _layout.ignoreLayout = true;
		}
	}
}
}