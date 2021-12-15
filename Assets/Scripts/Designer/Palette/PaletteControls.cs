using System.Collections;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Designer.Palette
{
public class PaletteControls : MonoBehaviour
{
	public BlockPalette Palette;
	public Transform BlockCategoryParent;
	public GameObject BlockCategoryButtonPrefab;
	public float HighlightFadeTime = 0.2f;

	public RectTransform BlockButtonFrame;

	private string _selectedCategoryId;
	private bool _started;

	private void OnEnable()
	{
		if (_started)
		{
			SetCategoryId(_selectedCategoryId, true);
		}
	}

	private IEnumerator Start()
	{
		foreach (var category in BlockDatabase.Instance.ListCategories())
		{
			GameObject buttonObject = Instantiate(BlockCategoryButtonPrefab, BlockCategoryParent);
			var button = buttonObject.GetComponent<BlockCategoryButton>();

			button.BlockCategoryId = category.Spec.BlockCategoryId;
			button.NameDisplay.text = category.Spec.DisplayName;
			if (TextureDatabase.Instance.HasTexture(category.Spec.IconTextureId))
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
		LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());

		// Give layout controller a tick to arrange things
		yield return null;

		float height = 0f;

		foreach (Transform child in transform)
		{
			var layoutElement = child.GetComponent<ILayoutElement>();

			if (layoutElement != null)
			{
				height += layoutElement.preferredHeight;
			}
		}

		GetComponent<RectTransform>().offsetMin = new Vector2(0f, -height);
		BlockButtonFrame.offsetMax = new Vector2(0f, -height);

		_started = true;
	}

	public void SetCategoryId(string value, bool immediate = false)
	{
		_selectedCategoryId = value;

		foreach (BlockCategoryButton button in GetComponentsInChildren<BlockCategoryButton>())
		{
			button.SelectHighlight.CrossFadeAlpha(
				button.BlockCategoryId == _selectedCategoryId ? 1f : 0f, immediate ? 0f : HighlightFadeTime, true
			);
		}

		Palette.SetSelectedCategory(_selectedCategoryId);
	}
}
}