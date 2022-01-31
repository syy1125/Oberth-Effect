namespace Syy1125.OberthEffect.Lib.Pid
{
public interface IPid<T>
{
	T Output { get; }
	void Update(T value, float deltaTime);
	void Reset();
}
}