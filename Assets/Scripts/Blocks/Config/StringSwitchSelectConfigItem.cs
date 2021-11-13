using System;

namespace Syy1125.OberthEffect.Blocks.Config
{
public class StringSwitchSelectConfigItem:ConfigItemBase
{
	public string[] Options;
	public Func<string, int> Deserialize;
	public Func<int, string> Serialize;
}
}