using System;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Spec;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;

namespace Syy1125.OberthEffect.Prototyping
{
public class BlockBuilderTest : MonoBehaviour
{
	public string BlockId;

	public Transform Parent;
	public Vector2Int RootPosition;
	public int Rotation;

	private void Awake()
	{
		ModLoader.Init();
		ModLoader.LoadModList();
		ModLoader.LoadAllEnabledContent();
	}

	private void Start()
	{
		BlockBuilder.BuildFromSpec(
			BlockDatabase.Instance.GetSpecInstance(BlockId).Spec, Parent, RootPosition, Rotation
		);
	}
}
}