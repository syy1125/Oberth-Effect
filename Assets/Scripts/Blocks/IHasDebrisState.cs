using Newtonsoft.Json.Linq;

namespace Syy1125.OberthEffect.Blocks
{
public interface IHasDebrisState
{
	JObject SaveDebrisState();
	void LoadDebrisState(JObject state);
}
}