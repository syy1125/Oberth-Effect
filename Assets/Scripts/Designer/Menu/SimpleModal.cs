using UnityEngine;

namespace Syy1125.OberthEffect.Designer.Menu
{
public class SimpleModal : MonoBehaviour, IModal
{
	public void OpenModal()
	{
		gameObject.SetActive(true);
	}

	public void CloseModal()
	{
		gameObject.SetActive(false);
	}
}
}