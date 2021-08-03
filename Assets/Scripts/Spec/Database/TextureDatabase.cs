using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Database
{
public class TextureDatabase : MonoBehaviour
{
	public static TextureDatabase Instance { get; private set; }

	private Dictionary<string, SpecInstance<TextureSpec>> _specs;
	private Dictionary<string, Sprite> _sprites;

	public Material VehicleBlockMaterial;
	public Material DefaultParticleMaterial;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else if (Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		_specs = ModLoader.AllTextures
			.ToDictionary(instance => instance.Spec.TextureId, instance => instance);
		_sprites = new Dictionary<string, Sprite>();
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public bool HasTexture(string textureId)
	{
		return _specs.ContainsKey(textureId);
	}

	public SpecInstance<TextureSpec> GetTextureSpec(string textureId)
	{
		return _specs[textureId];
	}

	public Sprite GetSprite(string textureId)
	{
		if (!_sprites.TryGetValue(textureId, out Sprite sprite))
		{
			var instance = _specs[textureId];

			// Reference: https://docs.unity3d.com/ScriptReference/ImageConversion.LoadImage.html
			var texture = new Texture2D(2, 2);
			texture.LoadImage(File.ReadAllBytes(instance.Spec.ImagePath));

			sprite = Sprite.Create(
				texture, new Rect(0f, 0f, texture.width, texture.height),
				instance.Spec.Pivot, instance.Spec.PixelsPerUnit
			);
			_sprites.Add(textureId, sprite);
		}

		return sprite;
	}
}
}