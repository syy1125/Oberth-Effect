﻿using Syy1125.OberthEffect.Components.UserInterface;
using Syy1125.OberthEffect.Spec.ModLoading;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.MainMenu
{
public class TutorialButtonGuard : MonoBehaviour
{
	private void OnEnable()
	{
		if (ModLoader.IsModded())
		{
			GetComponent<Button>().interactable = false;
			GetComponent<Tooltip>().SetTooltip("Tutorials are disabled in modded games.");
		}
	}
}
}