using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Colors;
using Syy1125.OberthEffect.Common.Match;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Components.UserInterface;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Lobby.Multiplayer
{
public class TeamColorPicker : MonoBehaviour
{
	[NonSerialized]
	public int ActiveTeamIndex;

	public Text Title;
	public RectTransform Panel;
	public ColorPicker Primary;
	public ColorPicker Secondary;
	public ColorPicker Tertiary;
	private ColorScheme _colorScheme;

	private void OnEnable()
	{
		if (!PhotonHelper.GetRoomGameMode().IsTeamMode())
		{
			Debug.LogError($"Team color picker is being opened on a non-team-based game mode!");
		}

		if (!PhotonNetwork.LocalPlayer.IsMasterClient)
		{
			Debug.LogError($"Team color picker is being opened on a non-master client!");
		}

		Title.text = $"Team {ActiveTeamIndex + 1} Colors";

		_colorScheme = PhotonTeamHelper.GetTeamColors(ActiveTeamIndex);

		Primary.InitColor(_colorScheme.PrimaryColor);
		Primary.OnChange.AddListener(SetPrimaryColor);
		Secondary.InitColor(_colorScheme.SecondaryColor);
		Secondary.OnChange.AddListener(SetSecondaryColor);
		Tertiary.InitColor(_colorScheme.TertiaryColor);
		Tertiary.OnChange.AddListener(SetTertiaryColor);

		if ((bool) PhotonNetwork.CurrentRoom.CustomProperties[PropertyKeys.USE_TEAM_COLORS])
		{
			SetAnchors(Primary, Vector2.zero, new Vector2(1f / 3f, 1f));
			Secondary.gameObject.SetActive(true);
			SetAnchors(Secondary, new Vector2(1f / 3f, 0f), new Vector2(2f / 3f, 1f));
			Tertiary.gameObject.SetActive(true);
			SetAnchors(Tertiary, new Vector2(2f / 3f, 0f), Vector2.one);
			Panel.sizeDelta = new Vector2(640, 300);
		}
		else
		{
			SetAnchors(Primary, Vector2.zero, Vector2.one);
			Secondary.gameObject.SetActive(false);
			Tertiary.gameObject.SetActive(false);
			Panel.sizeDelta = new Vector2(340, 300);
		}
	}

	private void OnDisable()
	{
		Primary.OnChange.RemoveListener(SetPrimaryColor);
		Secondary.OnChange.RemoveListener(SetSecondaryColor);
		Tertiary.OnChange.RemoveListener(SetTertiaryColor);
	}

	private static void SetAnchors(Component c, Vector2 min, Vector2 max)
	{
		var t = c.GetComponent<RectTransform>();
		t.anchorMin = min;
		t.anchorMax = max;
	}

	private void SetPrimaryColor(Color primary)
	{
		_colorScheme.PrimaryColor = primary;
	}

	private void SetSecondaryColor(Color secondary)
	{
		_colorScheme.SecondaryColor = secondary;
	}

	private void SetTertiaryColor(Color tertiary)
	{
		_colorScheme.TertiaryColor = tertiary;
	}

	public void ConfirmChanges()
	{
		string[] colors = (string[]) PhotonNetwork.CurrentRoom.CustomProperties[PropertyKeys.TEAM_COLORS];
		colors[ActiveTeamIndex] = ColorScheme.ToColorSet(_colorScheme);

		PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { PropertyKeys.TEAM_COLORS, colors } });
	}
}
}