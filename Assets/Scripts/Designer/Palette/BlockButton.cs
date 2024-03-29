using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Components.UserInterface;
using Syy1125.OberthEffect.Editor.PropertyDrawers;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Utils;
using Syy1125.OberthEffect.Spec;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Designer.Palette
{
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(Tooltip))]
public class BlockButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	private static readonly BlockContext PaletteContext = new()
	{
		IsMainVehicle = false,
		Environment = BlockEnvironment.Palette
	};

	[Header("References")]
	public RawImage PreviewImage;
	public Camera BlockCamera;
	public CanvasGroup Category;
	public Image CategoryIcon;
	public Text BlockName;

	[Header("Config")]
	public Color NormalColor;
	public Color HoverColor;
	public Color SelectedColor;
	public float FadeDuration;

	[UnityLayer]
	public int BlockRenderLayer;

	private string _blockId;

	private BlockPalette _controller;
	private Image _image;
	public Tooltip TooltipProvider { get; private set; }
	private bool _hover;

	private RenderTexture _rt;

	private bool _selected;

	private void Awake()
	{
		_controller = GetComponentInParent<BlockPalette>();
		_image = GetComponent<Image>();
		TooltipProvider = GetComponent<Tooltip>();
		GetComponent<Button>().onClick.AddListener(SelectBlock);
	}

	private void OnEnable()
	{
		_image.CrossFadeColor(NormalColor, 0, true, true);
	}

	private void Start()
	{
		_rt = new RenderTexture(100, 100, 0, RenderTextureFormat.Default);
		_rt.Create();

		PreviewImage.texture = _rt;
		BlockCamera.cullingMask = 1 << BlockRenderLayer;
		BlockCamera.targetTexture = _rt;
	}

	private void Update()
	{
		Category.alpha = Mathf.MoveTowards(Category.alpha, _hover ? 0f : 1f, Time.unscaledDeltaTime / FadeDuration);
	}

	public void DisplayBlock(SpecInstance<BlockSpec> instance)
	{
		_blockId = instance.Spec.BlockId;
		gameObject.name = _blockId;

		BlockName.text = instance.Spec.Info.ShortName;
		BlockCategorySpec category = BlockDatabase.Instance.GetCategory(instance.Spec.CategoryId);
		CategoryIcon.sprite = TextureDatabase.Instance.GetSprite(category.IconTextureId);

		GameObject previewObject = BlockBuilder.BuildFromSpec(
			instance.Spec, BlockCamera.transform, Vector2Int.zero, 0, PaletteContext
		);

		var previewTransform = previewObject.transform;
		Vector3 previewPosition = previewTransform.position;
		previewPosition.z = 0;
		previewTransform.position = previewPosition;

		Vector3 previewScale = previewTransform.lossyScale;
		BlockBounds bounds = new BlockBounds(
			instance.Spec.Construction.BoundsMin, instance.Spec.Construction.BoundsMax
		);
		float blockSize = Mathf.Max(bounds.Size.x, bounds.Size.y);
		previewTransform.localScale = new Vector3(
			0.8f / blockSize / previewScale.x,
			0.8f / blockSize / previewScale.y,
			1f
		);

		// Transform.Translate does not seem to work properly when localScale was just updated. 
		previewTransform.Translate(
			previewTransform.TransformVector(new Vector3(0.5f, 0.5f) - (Vector3) bounds.Center),
			Space.World
		);

		previewObject.AddComponent<OverrideOrderTooltip>().OverrideOrder = instance.OverrideOrder;

		LayerUtils.SetLayerRecursively(previewObject, BlockRenderLayer);

		string tooltip = TooltipProviderUtils.CombineTooltips(previewObject);
		TooltipProvider.SetTooltip(tooltip);
	}

	private void SelectBlock()
	{
		_controller.SelectBlock(_blockId);
		_image.CrossFadeColor(SelectedColor, FadeDuration, true, true);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		_hover = true;

		if (!_selected)
		{
			_image.CrossFadeColor(HoverColor, FadeDuration, true, true);
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		_hover = false;

		if (!_selected)
		{
			_image.CrossFadeColor(NormalColor, FadeDuration, true, true);
		}
	}

	public void OnSelect()
	{
		_selected = true;

		_image.CrossFadeColor(SelectedColor, FadeDuration, true, true);
	}

	public void OnDeselect()
	{
		_selected = false;

		if (!_hover)
		{
			_image.CrossFadeColor(NormalColor, FadeDuration, true, true);
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