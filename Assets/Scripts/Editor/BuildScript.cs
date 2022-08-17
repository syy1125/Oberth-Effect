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

	[MenuItem("Build/Build Windows Debug")]
	public static void BuildWindowsDebug()
	{
		BuildWindows(true);
	}

	private static BuildReport BuildWindows(bool debug)
	{
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

		return report;
	}

	private static BuildReport BuildMacOS(bool debug)
	{
		BuildReport report = BuildPipeline.BuildPlayer(
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

		return report;
	}

	private static BuildReport BuildLinux(bool debug)
	{
		BuildReport report = BuildPipeline.BuildPlayer(
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

		return report;
	}

	private static void BuildAll(bool debug)
	{
		BuildReport report = BuildWindows(debug);

		if (report.summary.result != BuildResult.Succeeded)
		{
			Debug.Log("Windows build did not succeed, aborting!");
			return;
		}

		report = BuildMacOS(debug);

		if (report.summary.result != BuildResult.Succeeded)
		{
			Debug.Log("MacOS build did not succeed, aborting!");
			return;
		}

		report = BuildLinux(debug);

		if (report.summary.result != BuildResult.Succeeded)
		{
			Debug.Log("Linux build did not succeed!");
			return;
		}
	}
}
}