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
public class ModBuilder
{
	public class BuildModWindow : EditorWindow
	{
		private static Assembly[] GetUserAssemblies()
		{
			var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies);
			return assemblies.Where(assembly => !assembly.name.StartsWith("Unity")).ToArray();
		}

		private Assembly[] _assemblies;
		[SerializeField]
		private int SelectedIndex;
		[SerializeField]
		private string ModFolder;
		[SerializeField]
		private string DllName;
		[SerializeField]
		private bool ChangeModJson;
		[SerializeField]
		private string EntryPointClass;

		private void Awake()
		{
			_assemblies = CompilationPipeline
				.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
				.Where(assembly => !assembly.name.StartsWith("Unity"))
				.Where(assembly => !assembly.name.StartsWith("Photon"))
				.Where(assembly => !assembly.name.StartsWith("YamlDotNet"))
				.ToArray();

			ChangeModJson = true;
		}

		private void OnGUI()
		{
			EditorGUI.BeginChangeCheck();
			SelectedIndex = EditorGUILayout.Popup(
				"Mod Assembly", SelectedIndex,
				_assemblies.Select(assembly => assembly.name).ToArray()
			);
			if (EditorGUI.EndChangeCheck())
			{
				var assembly = _assemblies[SelectedIndex];
				DllName = assembly.name;
				EntryPointClass = $"{assembly.rootNamespace}.{assembly.name.Split(".").Last()}";
			}

			ModFolder = EditorGUILayout.TextField("Mod Directory", ModFolder);
			DllName = EditorGUILayout.TextField("DLL Name", DllName);

			ChangeModJson = EditorGUILayout.Toggle("Update mod.json", ChangeModJson);
			if (ChangeModJson)
			{
				EntryPointClass = EditorGUILayout.TextField("Entry Point Class", EntryPointClass);
			}

			EditorGUILayout.Space();

			if (GUILayout.Button("Build"))
			{
				BuildMod();
			}
		}

		private void BuildMod()
		{
			EditorUtility.DisplayProgressBar("Building mod...", "Compiling scripts", 0f);

			var assembly = _assemblies[SelectedIndex];
			string modDirectory = Path.Join(Application.streamingAssetsPath, "Mods", ModFolder);
			if (!Directory.Exists(modDirectory)) Directory.CreateDirectory(modDirectory);

			var buildProc = Process.Start(
				"cmd.exe",
				$"/C dotnet build \"{assembly.name}.csproj\""
			);
			Debug.Assert(buildProc != null, nameof(buildProc) + " != null");
			buildProc.WaitForExit();

			EditorUtility.DisplayProgressBar("Building mod...", "Moving DLL", 0.8f);
			string buildDll = Path.Join(
				Application.dataPath, $"../Temp/Bin/Debug/{assembly.name}", assembly.name + ".dll"
			);
			string outputDll = Path.Join(modDirectory, Path.ChangeExtension(DllName, ".dll"));
			var renameProc = Process.Start("cmd.exe", $"/C copy \"{buildDll}\" \"{outputDll}\"");
			Debug.Assert(renameProc != null, nameof(renameProc) + " != null");

			renameProc.WaitForExit();

			if (ChangeModJson)
			{
				EditorUtility.DisplayProgressBar("Building mod...", "Updating mod.json", 0.99f);


				string modJsonFile = Path.Join(modDirectory, "mod.json");
				var modSpec = File.Exists(modJsonFile)
					? JsonUtility.FromJson<ModSpec>(File.ReadAllText(modJsonFile))
					: new ModSpec
					{
						DisplayName = ModFolder,
						Version = "v0.1.0",
						Description = "",
						AllowDuplicateDefs = false
					};

				modSpec.CodeModPath = Path.ChangeExtension(DllName, ".dll");
				modSpec.CodeModEntryPoint = EntryPointClass;

				File.WriteAllText(modJsonFile, JsonUtility.ToJson(modSpec, true));
			}

			EditorUtility.ClearProgressBar();
		}
	}

	[MenuItem("Build/Build Mods...")]
	public static void BuildMods()
	{
		var window = EditorWindow.GetWindow<BuildModWindow>();
		window.titleContent = new GUIContent("Oberth Effect Mod Builder");
		window.Show();
	}
}
}