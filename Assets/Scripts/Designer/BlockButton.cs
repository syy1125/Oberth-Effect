using System.Collections.Generic;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Common.UserInterface;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Designer
{
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(Tooltip))]
public class BlockButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public ColorBlock Colors;
	public Transform PreviewParent;
	public Text BlockName;

	private BlockPalette _controller;
	private Image _image;
	private Tooltip _tooltip;
	private bool _hover;

	private bool Selected => _controller.SelectedIndex == transform.GetSiblingIndex();

	private void Awake()
	{
		_controller = GetComponentInParent<BlockPalette>();
		_image = GetComponent<Image>();
		_tooltip = GetComponent<Tooltip>();
		GetComponent<Button>().onClick.AddListener(SelectBlock);
	}

	private void Start()
	{
		_image.CrossFadeColor(Colors.normalColor, 0, true, true);
	}


	public void DisplayBlock(GameObject block)
	{
		GameObject instance = Instantiate(block, PreviewParent);
		instance.transform.localScale = new Vector3(40, 40, 1);

		BlockInfo info = block.GetComponent<BlockInfo>();
		BlockName.text = info.ShortName;

		LinkedList<string> tooltips = new LinkedList<string>();
		foreach (MonoBehaviour behaviour in block.GetComponents<MonoBehaviour>())
		{
			if (behaviour is ITooltipProvider blockTooltip)
			{
				tooltips.AddLast(blockTooltip.GetTooltip());
			}
		}

		_tooltip.SetTooltip(string.Join("\n\n", tooltips));
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
}