using Syy1125.OberthEffect.Spec;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;

namespace Syy1125.OberthEffect.Init
{
// A debug counterpart to GameInitializer. It does everything synchronously in Awake.
public class DebugGameInitializer : MonoBehaviour
{
	private void Awake()
	{
		if (!ModLoader.DataReady)
		{
			ModLoader.Init();
			ModLoader.LoadModList();
			ModLoader.LoadAllEnabledContent();

			foreach (IGameContentDatabase database in GetComponents<IGameContentDatabase>())
			{
				database.Reload();
			}

			ModLoader.ComputeChecksum();

			KeybindManager.Instance.LoadKeybinds();
		}
	}
}
}