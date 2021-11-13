using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Utils;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Config
{
public interface IConfigComponent
{
	JObject ExportConfig();

	void InitDefaultConfig();

	// Note that, when importing, the config might be a partial one.
	// The component is expected to only update the options specified in the config if that is the case.
	void ImportConfig(JObject config);

	List<ConfigItemBase> GetConfigItems();
}

public static class BlockConfigHelper
{
	public static void SyncConfig(VehicleBlueprint.BlockInstance blockInstance, GameObject blockObject)
	{
		JObject config = ConfigUtils.ParseConfig(blockInstance.Config);

		foreach (MonoBehaviour behaviour in blockObject.GetComponents<MonoBehaviour>())
		{
			if (behaviour is IConfigComponent component)
			{
				component.InitDefaultConfig();

				string configKey = ConfigUtils.GetConfigKey(component.GetType());

				if (config.ContainsKey(configKey))
				{
					component.ImportConfig((JObject) config[configKey]);
				}

				config[configKey] = component.ExportConfig();
			}
		}

		blockInstance.Config = config.ToString(Formatting.None);
	}
	
	public static void LoadConfig(VehicleBlueprint.BlockInstance blockInstance, GameObject blockObject)
	{
		JObject config = ConfigUtils.ParseConfig(blockInstance.Config);

		foreach (MonoBehaviour behaviour in blockObject.GetComponents<MonoBehaviour>())
		{
			if (behaviour is IConfigComponent component)
			{
				component.InitDefaultConfig();
				string configKey = ConfigUtils.GetConfigKey(component.GetType());

				if (config.ContainsKey(configKey))
				{
					component.ImportConfig((JObject) config[configKey]);
				}
			}
		}
	}
}
}