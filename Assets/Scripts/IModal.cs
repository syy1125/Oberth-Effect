using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect
{
public interface IModal : IEventSystemHandler
{
	void OpenModal();
	void CloseModal();
}
}