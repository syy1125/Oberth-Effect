using System.Linq;
using Syy1125.OberthEffect.Common;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
[RequireComponent(typeof(SpriteRenderer))]
public class BlockColor : MonoBehaviour
{
	private static readonly int PrimaryColor = Shader.PropertyToID("_PrimaryColor");
	private static readonly int SecondaryColor = Shader.PropertyToID("_SecondaryColor");
	private static readonly int TertiaryColor = Shader.PropertyToID("_TertiaryColor");

	private ColorContext _context;
	private SpriteRenderer[] _sprites;
	private MaterialPropertyBlock _block;

	private void Awake()
	{
		_context = GetComponentInParent<ColorContext>();
		_sprites = GetComponentsInChildren<SpriteRenderer>();
		_block = new MaterialPropertyBlock();
	}

	private void OnEnable()
	{
		foreach (SpriteRenderer sprite in _sprites)
		{
			sprite.GetPropertyBlock(_block);
			_block.SetColor(PrimaryColor, _context.ColorScheme.PrimaryColor);
			_block.SetColor(SecondaryColor, _context.ColorScheme.SecondaryColor);
			_block.SetColor(TertiaryColor, _context.ColorScheme.TertiaryColor);
			sprite.SetPropertyBlock(_block);
		}

		_context.OnPrimaryColorChanged += UpdatePrimaryColor;
		_context.OnSecondaryColorChanged += UpdateSecondaryColor;
		_context.OnTertiaryColorChanged += UpdateTertiaryColor;
	}

	private void OnDisable()
	{
		_context.OnPrimaryColorChanged -= UpdatePrimaryColor;
		_context.OnSecondaryColorChanged -= UpdateSecondaryColor;
		_context.OnTertiaryColorChanged -= UpdateTertiaryColor;
	}

	private void UpdatePrimaryColor(Color color)
	{
		foreach (SpriteRenderer sprite in _sprites)
		{
			sprite.GetPropertyBlock(_block);
			_block.SetColor(PrimaryColor, color);
			sprite.SetPropertyBlock(_block);
		}
	}

	private void UpdateSecondaryColor(Color color)
	{
		foreach (SpriteRenderer sprite in _sprites)
		{
			sprite.GetPropertyBlock(_block);
			_block.SetColor(SecondaryColor, color);
			sprite.SetPropertyBlock(_block);
		}
	}

	private void UpdateTertiaryColor(Color color)
	{
		foreach (SpriteRenderer sprite in _sprites)
		{
			sprite.GetPropertyBlock(_block);
			_block.SetColor(TertiaryColor, color);
			sprite.SetPropertyBlock(_block);
		}
	}
}
}