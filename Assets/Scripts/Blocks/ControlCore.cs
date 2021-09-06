using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks
{
public interface IControlCoreRegistry : IBlockRegistry<ControlCore>, IEventSystemHandler
{}

public class ControlCore : MonoBehaviour
{
	private void OnEnable()
	{
		ExecuteEvents.ExecuteHierarchy<IControlCoreRegistry>(
			gameObject, null, (registry, _) => registry.RegisterBlock(this)
		);
	}

	private void OnDisable()
	{
		ExecuteEvents.ExecuteHierarchy<IControlCoreRegistry>(
			gameObject, null, (registry, _) => registry.UnregisterBlock(this)
		);
	}
}
}