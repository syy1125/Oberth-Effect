using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Colors;
using Syy1125.OberthEffect.Common.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Lobby.Multiplayer
{
public class TeamPanel : MonoBehaviourPunCallbacks, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
	public int TeamIndex { get; private set; }
	public Text TeamName;
	public GameObject DropOverlay;

	public Button ColorPickerButton;
	public Image PrimaryColor;
	public Image SecondaryColor;
	public Image TertiaryColor;

	[NonSerialized]
	public GameObject ColorPickerOverlay;

	public override void OnEnable()
	{
		base.OnEnable();

		DropOverlay.SetActive(false);
		ColorPickerButton.interactable = PhotonNetwork.LocalPlayer.IsMasterClient;
		ColorPickerButton.onClick.AddListener(OpenColorPicker);
	}

	public override void OnDisable()
	{
		base.OnDisable();

		ColorPickerButton.onClick.RemoveListener(OpenColorPicker);
	}

	public void SetTeamIndex(int teamIndex)
	{
		TeamIndex = teamIndex;
		UpdateDisplay();
	}

	private static void SetAnchors(Component c, Vector2 min, Vector2 max)
	{
		var t = c.GetComponent<RectTransform>();
		t.anchorMin = min;
		t.anchorMax = max;
	}

	public void UpdateDisplay()
	{
		TeamName.text = $"Team {TeamIndex + 1}";

		ColorScheme colorScheme = PhotonTeamHelper.GetTeamColors(TeamIndex);
		TeamName.color = colorScheme.PrimaryColor;

		PrimaryColor.color = colorScheme.PrimaryColor;
		SecondaryColor.color = colorScheme.SecondaryColor;
		TertiaryColor.color = colorScheme.TertiaryColor;

		if ((bool) PhotonNetwork.CurrentRoom.CustomProperties[PropertyKeys.USE_TEAM_COLORS])
		{
			SetAnchors(PrimaryColor, Vector2.zero, new Vector2(1f / 3f, 1f));
			SecondaryColor.gameObject.SetActive(true);
			SetAnchors(SecondaryColor, new Vector2(1f / 3f, 0f), new Vector2(2f / 3f, 1f));
			TertiaryColor.gameObject.SetActive(true);
			SetAnchors(TertiaryColor, new Vector2(2f / 3f, 0f), Vector2.one);
		}
		else
		{
			SetAnchors(PrimaryColor, Vector2.zero, Vector2.one);
			SecondaryColor.gameObject.SetActive(false);
			TertiaryColor.gameObject.SetActive(false);
		}
	}

	private static bool IsPlayerPanel(GameObject target)
	{
		return target != null && target.GetComponent<PlayerPanel>() != null;
	}

	public override void OnRoomPropertiesUpdate(Hashtable props)
	{
		if (props.ContainsKey(PropertyKeys.USE_TEAM_COLORS) || props.ContainsKey(PropertyKeys.TEAM_COLORS))
		{
			UpdateDisplay();
		}
	}

	public override void OnPlayerLeftRoom(Player player)
	{
		// Master may have changed
		ColorPickerButton.interactable = PhotonNetwork.LocalPlayer.IsMasterClient;
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

	private void OpenColorPicker()
	{
		ColorPickerOverlay.GetComponent<TeamColorPicker>().ActiveTeamIndex = TeamIndex;
		ColorPickerOverlay.SetActive(true);
	}
}
}