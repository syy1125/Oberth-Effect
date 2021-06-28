using System;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Designer.Menu
{
public class HelpPagination : MonoBehaviour
{
	public GameObject[] Pages;
	public Button LeftButton;
	public Button RightButton;
	public Text PageDisplay;

	private int _page;

	private void Awake()
	{
		_page = 0;
	}

	private void OnEnable()
	{
		UpdatePages();
		UpdateElements();

		LeftButton.onClick.AddListener(PageLeft);
		RightButton.onClick.AddListener(PageRight);
	}

	private void OnDisable()
	{
		LeftButton.onClick.RemoveListener(PageLeft);
		RightButton.onClick.RemoveListener(PageRight);
	}

	private void PageLeft()
	{
		_page = Mathf.Max(_page - 1, 0);

		UpdatePages();
		UpdateElements();
	}

	private void PageRight()
	{
		_page = Mathf.Min(_page + 1, Pages.Length - 1);

		UpdatePages();
		UpdateElements();
	}

	private void UpdatePages()
	{
		for (int i = 0; i < Pages.Length; i++)
		{
			Pages[i].SetActive(i == _page);
		}
	}

	private void UpdateElements()
	{
		LeftButton.interactable = _page > 0;
		RightButton.interactable = _page < Pages.Length - 1;
		PageDisplay.text = $"{_page + 1}/{Pages.Length}";
	}
}
}