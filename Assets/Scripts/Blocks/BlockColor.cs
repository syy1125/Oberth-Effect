using System;
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
	private SpriteRenderer _sprite;
	private MaterialPropertyBlock _block;

	private void Awake()
	{
		_context = GetComponentInParent<ColorContext>();
		_sprite = GetComponent<SpriteRenderer>();
		_block = new MaterialPropertyBlock();
	}

	private void OnEnable()
	{
		_sprite.GetPropertyBlock(_block);
		_block.SetColor(PrimaryColor, _context.PrimaryColor);
		_block.SetColor(SecondaryColor, _context.SecondaryColor);
		_block.SetColor(TertiaryColor, _context.TertiaryColor);
		_sprite.SetPropertyBlock(_block);

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
		_sprite.GetPropertyBlock(_block);
		_block.SetColor(PrimaryColor, color);
		_sprite.SetPropertyBlock(_block);
	}

	private void UpdateSecondaryColor(Color color)
	{
		_sprite.GetPropertyBlock(_block);
		_block.SetColor(SecondaryColor, color);
		_sprite.SetPropertyBlock(_block);
	}

	private void UpdateTertiaryColor(Color color)
	{
		_sprite.GetPropertyBlock(_block);
		_block.SetColor(TertiaryColor, color);
		_sprite.SetPropertyBlock(_block);
	}
}
}