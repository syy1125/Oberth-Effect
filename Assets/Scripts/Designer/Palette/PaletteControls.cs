using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Designer.Palette
{
public class PaletteControls : MonoBehaviour
{
	public BlockPalette Palette;
	public Transform BlockCategoryParent;
	public GameObject BlockCategoryButtonPrefab;
	public InputField SearchInput;
	public Button ClearSearchButton;
	public float HighlightFadeTime = 0.2f;

	private string _selectedCategoryId;
	private bool _started;

	private void OnEnable()
	{
		if (_started)
		{
			SetCategoryId(_selectedCategoryId, true);
			if (!string.IsNullOrWhiteSpace(SearchInput.text)) SetSearchString(SearchInput.text);
		}
	}

	private void Start()
	{
		foreach (var category in BlockDatabase.Instance.ListCategories())
		{
			GameObject buttonObject = Instantiate(BlockCategoryButtonPrefab, BlockCategoryParent);
			var button = buttonObject.GetComponent<BlockCategoryButton>();

			button.BlockCategoryId = category.Spec.BlockCategoryId;
			button.NameDisplay.text = category.Spec.DisplayName;
			if (TextureDatabase.Instance.ContainsId(category.Spec.IconTextureId))
			{
				button.Icon.sprite = TextureDatabase.Instance.GetSprite(category.Spec.IconTextureId);
			}
			else
			{
				button.Icon.gameObject.SetActive(false);
				button.NameDisplay.GetComponent<RectTransform>().offsetMin = Vector2.zero;
			}
		}

		SetCategoryId("");

		SearchInput.onValueChanged.AddListener(SetSearchString);
		ClearSearchButton.onClick.AddListener(ClearSearchString);
		ClearSearchButton.interactable = false;

		_started = true;
	}

	public void SetCategoryId(string value, bool skipAnimate = false)
	{
		_selectedCategoryId = value;

		float fadeTime = skipAnimate ? 0f : HighlightFadeTime;

		foreach (BlockCategoryButton button in GetComponentsInChildren<BlockCategoryButton>())
		{
			button.SelectHighlight.CrossFadeAlpha(
				button.BlockCategoryId == _selectedCategoryId ? 1f : 0f, fadeTime, true
			);
		}

		Palette.SetSelectedCategory(_selectedCategoryId);
	}

	private void SetSearchString(string search)
	{
		if (!string.IsNullOrWhiteSpace(search))
		{
			ClearSearchButton.interactable = true;
			SetCategoryId("");
			Palette.SetSearchString(search);
		}
		else
		{
			ClearSearchButton.interactable = false;
			Palette.SetSelectedCategory(_selectedCategoryId);
		}
	}

	private void ClearSearchString()
	{
		SearchInput.text = "";
	}
}
}