using Photon.Pun;
using Syy1125.OberthEffect.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Syy1125.OberthEffect.Simulation
{
public class SimulationMenu : MonoBehaviourPunCallbacks
{
	public InputActionReference MenuAction;

	private bool _open;

	public override void OnEnable()
	{
		base.OnEnable();
		_open = false;
		MenuAction.action.Enable();
		MenuAction.action.performed += ToggleMenu;
	}

	public override void OnDisable()
	{
		base.OnDisable();
		MenuAction.action.performed -= ToggleMenu;
		MenuAction.action.Disable();
	}

	private void ToggleMenu(InputAction.CallbackContext context)
	{
		_open = !_open;

		foreach (Transform child in transform)
		{
			child.gameObject.SetActive(_open);
		}
	}

	public void LeaveGame()
	{
		PhotonNetwork.LeaveRoom();
	}

	public override void OnLeftRoom()
	{
		PhotonHelper.ClearPhotonPlayerProperties();
		SceneManager.LoadScene("Scenes/Multiplayer Lobby");
	}
}
}