using Newtonsoft.Json.Linq;

namespace Syy1125.OberthEffect.Blocks
{
public interface IConfigComponent
{
	JObject ExportConfig();

	void InitDefaultConfig();

	// Note that, when importing, the config might be a partial one.
	// The component is expected to only update the options specified in the config if that is the case.
	void ImportConfig(JObject config);
}
}