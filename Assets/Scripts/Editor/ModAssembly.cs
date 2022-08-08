using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Syy1125.OberthEffect.Spec.ModLoading;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Syy1125.OberthEffect.Editor
{
[CreateAssetMenu(fileName = "ModAssembly", menuName = "Mod Assembly", order = 0)]
public class ModAssembly : ScriptableObject
{
	public string Assembly;
	public string ModFolder;
	public string DllName;
	public bool ChangeModJson;
	public string EntryPointClass;
}

[CustomEditor(typeof(ModAssembly))]
public class ModAssemblyEditor : UnityEditor.Editor
{
	private Assembly[] _assemblies;
	private SerializedProperty _assembly;
	private SerializedProperty _modFolder;
	private SerializedProperty _dllName;
	private SerializedProperty _changeModJson;
	private SerializedProperty _entryPointClass;

	private void OnEnable()
	{
		_assemblies = CompilationPipeline.GetAssemblies()
			// Remove library assemblies
			.Where(assembly => !assembly.name.StartsWith("Unity"))
			.Where(assembly => !assembly.name.StartsWith("Photon"))
			.Where(assembly => !assembly.name.StartsWith("YamlDotNet"))
			.ToArray();

		_assembly = serializedObject.FindProperty(nameof(ModAssembly.Assembly));
		_modFolder = serializedObject.FindProperty(nameof(ModAssembly.ModFolder));
		_dllName = serializedObject.FindProperty(nameof(ModAssembly.DllName));
		_changeModJson = serializedObject.FindProperty(nameof(ModAssembly.ChangeModJson));
		_entryPointClass = serializedObject.FindProperty(nameof(ModAssembly.EntryPointClass));
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		int currentIndex = Array.FindIndex(_assemblies, assembly => assembly.name == _assembly.stringValue);
		if (currentIndex < 0) currentIndex = 0;
		int newIndex = EditorGUILayout.Popup(
			"Mod Assembly", currentIndex, _assemblies.Select(assembly => assembly.name).ToArray()
		);
		if (currentIndex != newIndex)
		{
			_assembly.stringValue = _assemblies[newIndex].name;
		}

		EditorGUILayout.PropertyField(_modFolder);
		EditorGUILayout.PropertyField(_dllName);
		EditorGUILayout.PropertyField(_changeModJson);

		if (_changeModJson.boolValue)
		{
			EditorGUILayout.PropertyField(_entryPointClass);
		}

		serializedObject.ApplyModifiedProperties();

		EditorGUILayout.Space();

		if (GUILayout.Button("Build"))
		{
			BuildMod();
		}
	}

	private void BuildMod()
	{
		EditorUtility.DisplayProgressBar("Building mod...", "Compiling scripts", 0f);

		var modAssembly = (ModAssembly) serializedObject.targetObject;

		var assembly = _assemblies.FirstOrDefault(assembly => assembly.name == modAssembly.Assembly);
		string modDirectory = Path.Combine(Application.streamingAssetsPath, "Mods", modAssembly.ModFolder);
		if (!Directory.Exists(modDirectory)) Directory.CreateDirectory(modDirectory);

		string projectPath = Path.Combine(Application.dataPath, "..", $"{assembly.name}.csproj");
		var buildProc = Process.Start(
			"cmd.exe",
			$"/C dotnet build \"{projectPath}\""
		);
		Debug.Assert(buildProc != null, nameof(buildProc) + " != null");
		buildProc.WaitForExit();

		EditorUtility.DisplayProgressBar("Building mod...", "Moving DLL", 0.8f);
		string buildDll = Path.Combine(
			Application.dataPath, $"../Temp/Bin/Debug/{assembly.name}", assembly.name + ".dll"
		);
		string outputDll = Path.Combine(modDirectory, Path.ChangeExtension(modAssembly.DllName, ".dll")!);
		var renameProc = Process.Start("cmd.exe", $"/C copy \"{buildDll}\" \"{outputDll}\"");
		Debug.Assert(renameProc != null, nameof(renameProc) + " != null");

		renameProc.WaitForExit();

		if (modAssembly.ChangeModJson)
		{
			EditorUtility.DisplayProgressBar("Building mod...", "Updating mod.json", 0.99f);


			string modJsonFile = Path.Combine(modDirectory, "mod.json");
			var modSpec = File.Exists(modJsonFile)
				? JsonUtility.FromJson<ModSpec>(File.ReadAllText(modJsonFile))
				: new()
				{
					DisplayName = modAssembly.ModFolder,
					Version = "v0.1.0",
					Description = "",
					AllowDuplicateDefs = false
				};

			modSpec.CodeModPath = Path.ChangeExtension(modAssembly.DllName, ".dll");
			modSpec.CodeModEntryPoint = modAssembly.EntryPointClass;

			File.WriteAllText(modJsonFile, JsonUtility.ToJson(modSpec, true));
		}

		EditorUtility.ClearProgressBar();
	}
}
}