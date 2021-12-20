using Syy1125.OberthEffect.Common.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Lobby.Multiplayer
{
public class TeamPanel : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
	public int TeamIndex { get; private set; }
	public Text TeamName;
	public GameObject DropOverlay;

	private void OnEnable()
	{
		DropOverlay.SetActive(false);
	}

	public void SetTeamIndex(int teamIndex)
	{
		TeamIndex = teamIndex;
		UpdateNameDisplay();
	}

	public void UpdateNameDisplay()
	{
		TeamName.text = $"Team {TeamIndex + 1}";
		TeamName.color = PhotonTeamHelper.GetTeamColors(TeamIndex).PrimaryColor;
	}

	private static bool IsPlayerPanel(GameObject target)
	{
		return target != null && target.GetComponent<PlayerPanel>() != null;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (IsPlayerPanel(eventData.pointerDrag))
		{
			DropOverlay.transform.SetAsLastSibling();
			DropOverlay.SetActive(true);
		}
	}

	public void OnDrop(PointerEventData eventData)
	{
		if (!IsPlayerPanel(eventData.pointerDrag)) return;

		var player = eventData.pointerDrag.GetComponent<PlayerPanel>().Player;
		PhotonTeamHelper.SetPlayerTeam(player, TeamIndex);

		eventData.pointerDrag.transform.SetParent(transform);
		DropOverlay.SetActive(false);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		DropOverlay.SetActive(false);
	}
}
}