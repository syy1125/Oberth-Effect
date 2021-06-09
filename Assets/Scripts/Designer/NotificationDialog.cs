using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Designer
{
public class NotificationDialog : MonoBehaviour, IModal
{
	public Text ContentText;

	public void SetContent(string content)
	{
		ContentText.text = content;
	}

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