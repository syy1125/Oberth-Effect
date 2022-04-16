using Photon.Pun;
using Photon.Realtime;
using Syy1125.OberthEffect.Foundation.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(HighlightTarget))]
public class HighlightPlayer : MonoBehaviour
{
	public RectTransform Pointer;

	private CanvasGroup _group;
	private HighlightTarget _highlightTarget;

	private void Awake()
	{
		_group = GetComponent<CanvasGroup>();
		_highlightTarget = GetComponent<HighlightTarget>();
	}

	private void Start()
	{
		Player targetPlayer = _highlightTarget.Target.GetComponent<PhotonView>().Owner;
		GetComponentInChildren<Text>().text = targetPlayer.NickName;

		Color playerColor = PhotonTeamHelper.GetPlayerTeamColors(targetPlayer).PrimaryColor;
		foreach (Graphic graphic in GetComponentsInChildren<Graphic>())
		{
			graphic.color = playerColor;
		}
	}

	private void LateUpdate()
	{
		if (_highlightTarget.OnScreen)
		{
			_group.alpha = 0f;
			_group.interactable = false;
			_group.blocksRaycasts = false;
			return;
		}

		_group.alpha = 1f;
		_group.interactable = true;
		_group.blocksRaycasts = true;

		Pointer.rotation = Quaternion.LookRotation(
			Vector3.forward, _highlightTarget.ScreenPosition - new Vector2(0.5f, 0.5f)
		);
	}
}
}