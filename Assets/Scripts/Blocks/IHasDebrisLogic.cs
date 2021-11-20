
using Newtonsoft.Json.Linq;

namespace Syy1125.OberthEffect.Blocks
{
public interface IHasDebrisLogic
{
	public JObject SaveDebrisState();
	public void LoadDebrisState(JObject state);
}
}