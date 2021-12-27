﻿using System;
using System.Collections;
using System.Threading.Tasks;
using Syy1125.OberthEffect.Components.UserInterface;
using Syy1125.OberthEffect.Editor.PropertyDrawers;
using Syy1125.OberthEffect.Spec;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Init
{
public class GameInitializer : MonoBehaviour
{
	public SceneReference MainMenuScene;
	public Text LoadText;
	public ProgressBar LoadProgress;

	[TagField]
	public string DatabaseTag;

	private IEnumerator Start()
	{
		ModLoader.Init();

		Task loadTask = Task.Run(
			() =>
			{
				ModLoader.LoadModList();
				ModLoader.LoadAllEnabledContent();
			}
		);

		while (!loadTask.IsCompleted)
		{
			lock (ModLoader.LoadStateLock)
			{
				Tuple<int, int> progress = ModLoader.LoadProgress;

				switch (ModLoader.LoadState)
				{
					case ModLoader.State.LoadModList:
						LoadText.text = "Loading mod list";
						LoadProgress.Progress = null;
						break;
					case ModLoader.State.LoadDocuments:
						LoadText.text = $"Loading mods: {ModLoader.LoadDescription}";
						LoadProgress.Progress = progress;
						break;
					case ModLoader.State.ParseDocuments:
						LoadText.text = $"Parsing documents {progress.Item1} of {progress.Item2}";
						LoadProgress.Progress = progress;
						break;
					case ModLoader.State.ValidateDocuments:
						LoadText.text = "Validating game data";
						LoadProgress.Progress = null;
						break;
					default:
						LoadText.text = "";
						LoadProgress.Progress = null;
						break;
				}
			}

			yield return null;
		}

		if (loadTask.IsFaulted)
		{
			Debug.LogError("Game encountered error when loading:");
			Debug.LogError(loadTask.Exception);
		}

		LoadText.text = "Initializing database";
		LoadProgress.Progress = null;

		IGameContentDatabase[] databases = GameObject.FindWithTag(DatabaseTag).GetComponents<IGameContentDatabase>();

		Task dbTask = Task.Run(
			() =>
			{
				foreach (IGameContentDatabase database in databases)
				{
					database.Reload();
				}
			}
		);

		yield return new WaitUntil(() => dbTask.IsCompleted);

		LoadText.text = "Loading textures";
		LoadProgress.Progress = null;

		Task textureTask = Task.Run(TextureDatabase.Instance.LoadTextures);
		yield return new WaitUntil(() => textureTask.IsCompleted);

		LoadText.text = "Finalizing";
		LoadProgress.Progress = null;

		Task checksumTask = Task.Run(ModLoader.ComputeChecksum);
		yield return new WaitUntil(() => checksumTask.IsCompleted);

		LoadText.text = "Starting game";
		LoadProgress.Progress = null;

		SceneManager.LoadSceneAsync(MainMenuScene);
	}
}
}