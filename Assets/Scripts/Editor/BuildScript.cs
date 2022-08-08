using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Syy1125.OberthEffect.Editor
{
public static class BuildScript
{
	private static string[] GetBuildScenes()
	{
		return EditorBuildSettings.scenes.Select(scene => scene.path).ToArray();
	}

	[MenuItem("Build/Build All Debug")]
	public static void BuildAllDebug()
	{
		BuildAll(true);
	}

	public static void BuildAllRelease()
	{
		BuildAll(false);
	}

	private static void BuildAll(bool debug)
	{
		// Build Windows
		BuildReport report = BuildPipeline.BuildPlayer(
			new()
			{
				scenes = GetBuildScenes(),
				locationPathName = Path.Combine("Builds", "OberthEffect_Windows", "Oberth Effect.exe"),
				target = BuildTarget.StandaloneWindows64,
				options = debug ? BuildOptions.Development : BuildOptions.None
			}
		);

		if (report.summary.result == BuildResult.Succeeded)
		{
			File.Delete(
				Path.Combine(
					"Builds", "OberthEffect_Windows", "Oberth Effect_Data", "Managed",
					"Syy1125.OberthEffect.CoreMod.dll"
				)
			);

			if (debug)
			{
				File.Delete(
					Path.Combine(
						"Builds", "OberthEffect_Windows", "Oberth Effect_Data", "Managed",
						"Syy1125.OberthEffect.CoreMod.pdb"
					)
				);
			}
		}
		else
		{
			Debug.Log("Windows build did not succeed, aborting!");
			return;
		}

		// Build MacOS
		report = BuildPipeline.BuildPlayer(
			new()
			{
				scenes = GetBuildScenes(),
				locationPathName = Path.Combine("Builds", "OberthEffect_MacOS.app"),
				target = BuildTarget.StandaloneOSX,
				options = debug ? BuildOptions.Development : BuildOptions.None
			}
		);

		if (report.summary.result == BuildResult.Succeeded)
		{
			File.Delete(
				Path.Combine(
					"Builds", "OberthEffect_MacOS.app", "Contents", "Resources", "Data", "Managed",
					"Syy1125.OberthEffect.CoreMod.dll"
				)
			);

			if (debug)
			{
				File.Delete(
					Path.Combine(
						"Builds", "OberthEffect_MacOS.app", "Contents", "Resources", "Data", "Managed",
						"Syy1125.OberthEffect.CoreMod.pdb"
					)
				);
			}
		}
		else
		{
			Debug.Log("MacOS build did not succeed, aborting!");
			return;
		}

		// Build Linux
		report = BuildPipeline.BuildPlayer(
			new()
			{
				scenes = GetBuildScenes(),
				locationPathName = Path.Combine("Builds", "OberthEffect_Linux", "Oberth Effect.x86_64"),
				target = BuildTarget.StandaloneLinux64,
				options = debug ? BuildOptions.Development : BuildOptions.None
			}
		);

		if (report.summary.result == BuildResult.Succeeded)
		{
			File.Delete(
				Path.Combine(
					"Builds", "OberthEffect_Linux", "Oberth Effect_Data", "Managed", "Syy1125.OberthEffect.CoreMod.dll"
				)
			);

			if (debug)
			{
				File.Delete(
					Path.Combine(
						"Builds", "OberthEffect_Linux", "Oberth Effect_Data", "Managed",
						"Syy1125.OberthEffect.CoreMod.pdb"
					)
				);
			}
		}
		else
		{
			Debug.Log("Linux build did not succeed!");
			return;
		}
	}
}
}