using System;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Designer.Palette
{
public class BlockCategoryButton : MonoBehaviour
{
	[NonSerialized]
	public string BlockCategoryId = "";
	public Image Icon;
	public Text NameDisplay;
	public Image SelectHighlight;

	public void HandleClick()
	{
		GetComponentInParent<PaletteControls>().SetCategoryId(BlockCategoryId);
	}
}
}