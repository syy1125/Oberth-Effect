using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
public class ResourceGeneratorBlock : MonoBehaviour
{
	public virtual float GenerateEnergy()
	{
		return 0f;
	}

	public virtual float GenerateFuel()
	{
		return 0f;
	}
}
}