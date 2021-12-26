using System;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Components.UserInterface
{
public class ProgressBar : MonoBehaviour
{
	public Material IndeterminateMaterial;
	public Image FillBar;
	public Text ProgressText;

	[SerializeField]
	private Tuple<int, int> _progress;

	public Tuple<int, int> Progress
	{
		get => _progress;
		set
		{
			_progress = value;
			Init();
		}
	}

	private void Start()
	{
		Init();
	}

	private void Init()
	{
		if (_progress == null)
		{
			FillBar.material = IndeterminateMaterial;
			FillBar.fillAmount = 1f;

			ProgressText.text = "";
		}
		else
		{
			FillBar.material = null;
			FillBar.fillAmount = (float) _progress.Item1 / _progress.Item2;

			ProgressText.text = $"{_progress.Item1} / {_progress.Item2}";
		}
	}
}
}