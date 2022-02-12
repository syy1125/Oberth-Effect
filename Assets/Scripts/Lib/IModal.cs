using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Lib
{
public interface IModal : IEventSystemHandler
{
	void OpenModal();
	void CloseModal();
}
}