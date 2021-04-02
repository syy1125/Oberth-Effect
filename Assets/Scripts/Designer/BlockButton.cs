using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class BlockButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public ColorBlock Colors;

	private BlockPalette _controller;
	private Image _image;
	private bool _hover;

	private bool Selected => _controller.SelectedIndex == transform.GetSiblingIndex();

	private void Awake()
	{
		_controller = GetComponentInParent<BlockPalette>();
		_image = GetComponent<Image>();
		GetComponent<Button>().onClick.AddListener(SelectBlock);
	}

	private void Start()
	{
		_image.CrossFadeColor(Colors.normalColor, 0, true, true);
	}


	public void DisplayBlock(GameObject block)
	{
		GameObject instance = Instantiate(block, transform);
		instance.transform.localScale = new Vector3(40, 40, 1);

		foreach (BlockBehaviour behaviour in instance.GetComponents<BlockBehaviour>())
		{
			behaviour.InDesigner = true;
		}
	}

	private void SelectBlock()
	{
		_controller.SelectBlockIndex(transform.GetSiblingIndex());
		_image.CrossFadeColor(Colors.selectedColor, Colors.fadeDuration, true, true);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		_hover = true;
		if (!Selected)
		{
			_image.CrossFadeColor(Colors.highlightedColor, Colors.fadeDuration, true, true);
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		_hover = false;
		if (!Selected)
		{
			_image.CrossFadeColor(Colors.normalColor, Colors.fadeDuration, true, true);
		}
	}

	public void OnDeselect()
	{
		if (!_hover)
		{
			_image.CrossFadeColor(Colors.normalColor, Colors.fadeDuration, true, true);
		}
	}
}