namespace Syy1125.OberthEffect.Spec.Database
{
public interface IGameContentDatabase
{
	void Reload();

	bool ContainsId(string id);
}
}