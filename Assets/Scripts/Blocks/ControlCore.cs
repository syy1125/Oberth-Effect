using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
public interface IControlCoreRegistry : IBlockRegistry<ControlCore>
{}

public class ControlCore : MonoBehaviour
{
	private void OnEnable()
	{
		GetComponentInParent<IControlCoreRegistry>()?.RegisterBlock(this);
	}

	private void OnDisable()
	{
		GetComponentInParent<IControlCoreRegistry>()?.UnregisterBlock(this);
	}
}
}