using System.Collections.Generic;
using System.IO;
using System.Linq;
using Syy1125.OberthEffect.Spec.ModLoading;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Database
{
public class TextureDatabase : MonoBehaviour, IGameContentDatabase
{
	public static TextureDatabase Instance { get; private set; }

	private Dictionary<string, SpecInstance<TextureSpec>> _specs;
	private Dictionary<string, Sprite> _sprites;

	public Material VehicleBlockMaterial;
	public Material DefaultParticleMaterial;
	public Material DefaultLineMaterial;

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
	}

	public void Reload()
	{
		_specs = ModLoader.TexturePipeline.Results
			.ToDictionary(instance => instance.Spec.TextureId, instance => instance);
		_sprites = new Dictionary<string, Sprite>();
		Debug.Log($"Loaded {_specs.Count} texture specs");
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void LoadTextures()
	{
		foreach (string textureId in _specs.Keys)
		{
			if (!_sprites.ContainsKey(textureId))
			{
				_sprites.Add(textureId, LoadSprite(textureId));
			}
		}
	}

	public bool ContainsId(string textureId)
	{
		return textureId != null && _specs.ContainsKey(textureId);
	}

	public SpecInstance<TextureSpec> GetTextureSpec(string textureId)
	{
		return _specs[textureId];
	}

	public Sprite GetSprite(string textureId)
	{
		if (!_sprites.TryGetValue(textureId, out Sprite sprite))
		{
			sprite = LoadSprite(textureId);
			_sprites.Add(textureId, sprite);
		}

		return sprite;
	}

	private Sprite LoadSprite(string textureId)
	{
		var instance = _specs[textureId];

		// Reference: https://docs.unity3d.com/ScriptReference/ImageConversion.LoadImage.html
		var texture = new Texture2D(2, 2);
		texture.LoadImage(File.ReadAllBytes(instance.Spec.ImagePath));

		var sprite = Sprite.Create(
			texture, new Rect(0f, 0f, texture.width, texture.height),
			instance.Spec.Pivot, instance.Spec.PixelsPerUnit
		);
		return sprite;
	}
}
}