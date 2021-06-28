using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Common.UserInterface;
using Syy1125.OberthEffect.Editor.PropertyDrawers;
using Syy1125.OberthEffect.Utils;
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
	public RawImage PreviewImage;
	public Camera BlockCamera;
	public Text BlockName;

	[UnityLayer]
	public int BlockRenderLayer;

	private BlockPalette _controller;
	private Image _image;
	private Tooltip _tooltip;
	private bool _hover;

	private RenderTexture _rt;

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

		_rt = new RenderTexture(100, 100, 0, RenderTextureFormat.Default);
		_rt.Create();

		PreviewImage.texture = _rt;
		BlockCamera.cullingMask = 1 << BlockRenderLayer;
		BlockCamera.targetTexture = _rt;
	}


	public void DisplayBlock(GameObject block)
	{
		GameObject instance = Instantiate(block, BlockCamera.transform);

		Vector3 instancePosition = instance.transform.position;
		instancePosition.z = 0;
		instance.transform.position = instancePosition;

		Vector3 instanceScale = instance.transform.lossyScale;
		instance.transform.localScale = new Vector3(0.8f / instanceScale.x, 0.8f / instanceScale.y, 1f);

		LayerUtils.SetLayerRecursively(instance, BlockRenderLayer);

		BlockInfo info = block.GetComponent<BlockInfo>();
		BlockName.text = info.ShortName;

		string tooltip = TooltipProviderUtils.CombineTooltips(block);

		_tooltip.SetTooltip(tooltip);
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

	private void OnDestroy()
	{
		if (_rt != null)
		{
			_rt.Release();
		}
	}
}
}