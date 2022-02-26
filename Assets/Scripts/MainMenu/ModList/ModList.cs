using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Spec.ModLoading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.MainMenu.ModList
{
[RequireComponent(typeof(ScrollRect))]
public class ModList : MonoBehaviour
{
	public SceneReference LoadingScene;
	public GameObject ListItemPrefab;
	public Button ResetButton;
	public Button ConfirmButton;

	private ScrollRect _scroll;
	private List<ModListElement> _modList;
	public int ModCount => _modList.Count;

	private void Awake()
	{
		_scroll = GetComponent<ScrollRect>();
	}

	private void OnEnable()
	{
		ReloadModList();

		ResetButton.onClick.AddListener(ReloadModList);
		ConfirmButton.onClick.AddListener(ConfirmModList);
	}

	private void OnDisable()
	{
		ResetButton.onClick.RemoveListener(ReloadModList);
		ConfirmButton.onClick.RemoveListener(ConfirmModList);
	}

	private void ReloadModList()
	{
		_modList = new List<ModListElement>(ModLoader.AllMods);

		foreach (Transform child in _scroll.content)
		{
			Destroy(child.gameObject);
		}

		foreach (ModListElement element in _modList)
		{
			GameObject listItem = Instantiate(ListItemPrefab, _scroll.content);
			listItem.GetComponent<ModListItem>().LoadMod(element);
		}

		ResetButton.interactable = false;
		ConfirmButton.interactable = false;
	}

	public void SetModEnabled(int modIndex, bool modEnabled)
	{
		if (modEnabled == _modList[modIndex].Enabled) return;

		var modElement = _modList[modIndex];
		modElement.Enabled = modEnabled;
		_modList[modIndex] = modElement;

		ResetButton.interactable = true;
		ConfirmButton.interactable = IsModConfigurationValid();
	}

	public void MoveModIndex(int oldIndex, int newIndex)
	{
		if (newIndex == oldIndex) return;

		Debug.Assert(oldIndex >= 0, "oldIndex >= 0");
		Debug.Assert(oldIndex < _modList.Count, "oldIndex < _modList.Count");
		Debug.Assert(newIndex >= 0, "newIndex >= 0");
		Debug.Assert(newIndex < _modList.Count, "newIndex < _modList.Count");

		Debug.Log($"Mod list has {_modList.Count} elements; moving {oldIndex} to {newIndex}.");
		ModListElement element = _modList[oldIndex];
		_modList.RemoveAt(oldIndex);
		_modList.Insert(newIndex, element);

		ResetButton.interactable = true;
		ConfirmButton.interactable = IsModConfigurationValid();
	}

	private bool IsModConfigurationValid()
	{
		return _modList.Any(mod => mod.Enabled);
	}

	private void ConfirmModList()
	{
		ModLoader.SaveModList(_modList);
		SceneManager.LoadScene(LoadingScene);
	}
}
}