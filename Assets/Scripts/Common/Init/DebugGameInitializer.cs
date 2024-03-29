﻿using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Spec.ModLoading;
using UnityEngine;

namespace Syy1125.OberthEffect.Common.Init
{
// A debug counterpart to GameInitializer. It does most things synchronously in Awake.
public class DebugGameInitializer : MonoBehaviour
{
	public TextAsset CoreBlock;
	public TextAsset CoreArmor;

	private bool _debugInit;

	private void Awake()
	{
		if (!ModLoader.DataReady)
		{
			Debug.Log("Using debug game initialization!");
			_debugInit = true;

			ModLoader.Init();

			ModLoader.InjectBlockSpec(CoreBlock);
			ModLoader.InjectArmorTypeSpec(CoreArmor);

			ModLoader.LoadModList();
			ModLoader.LoadAllEnabledContent();

			foreach (IGameContentDatabase database in GetComponents<IGameContentDatabase>())
			{
				database.Reload();
			}

			ModLoader.ComputeChecksum();
		}
	}

	private void Start()
	{
		if (_debugInit)
		{
			KeybindManager.Instance.QuickLoadKeybinds();
			AudioMixerManager.Instance.LoadVolumes();
			StartCoroutine(SoundDatabase.Instance.LoadAudioClips());
		}
	}
}
}